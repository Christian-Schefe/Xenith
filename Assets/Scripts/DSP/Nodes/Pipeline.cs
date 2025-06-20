
using System.Collections.Generic;
using System.Linq;

namespace DSP
{
    public class Pipe
    {
        private readonly NamedValue[] sourceOutputs;
        private readonly NamedValue[] targetInputs;

        public Pipe(AudioNode source, AudioNode target)
        {
            if (source == null || target == null)
            {
                throw new System.ArgumentNullException("Source and target nodes cannot be null.");
            }

            sourceOutputs = source.BuildOutputs().ToArray();
            targetInputs = target.BuildInputs().ToArray();
            if (sourceOutputs.Length != targetInputs.Length)
            {
                throw new System.ArgumentException("Source outputs count must match target inputs count.");
            }
            for (int i = 0; i < sourceOutputs.Length; i++)
            {
                if (sourceOutputs[i].Value.Type != targetInputs[i].Value.Type)
                {
                    throw new System.ArgumentException($"Type mismatch at index {i}: {sourceOutputs[i].Value.Type} vs {targetInputs[i].Value.Type}.");
                }
            }
        }

        public Pipe(AudioNode source, int sourceOffset, AudioNode target, int targetOffset, int count)
        {
            if (source == null || target == null)
            {
                throw new System.ArgumentNullException("Source and target nodes cannot be null.");
            }
            if (sourceOffset < 0 || targetOffset < 0 || count <= 0)
            {
                throw new System.ArgumentOutOfRangeException("Offsets and count must be non-negative and count must be greater than zero.");
            }
            var sourceOutputList = source.BuildOutputs();
            var targetInputList = target.BuildInputs();
            if (sourceOffset + count > sourceOutputList.Count || targetOffset + count > targetInputList.Count)
            {
                throw new System.ArgumentException("Offsets and count exceed the available outputs or inputs.");
            }
            sourceOutputs = sourceOutputList.Skip(sourceOffset).Take(count).ToArray();
            targetInputs = targetInputList.Skip(targetOffset).Take(count).ToArray();

            for (int i = 0; i < sourceOutputs.Length; i++)
            {
                if (sourceOutputs[i].Value.Type != targetInputs[i].Value.Type)
                {
                    throw new System.ArgumentException($"Type mismatch at index {i}: {sourceOutputs[i].Value.Type} vs {targetInputs[i].Value.Type}.");
                }
            }
        }

        public void Transfer()
        {
            for (int i = 0; i < sourceOutputs.Length; i++)
            {
                targetInputs[i].Set(sourceOutputs[i]);
            }
        }
    }

    public class Pipeline : AudioNode
    {
        private readonly AudioNode[] nodes;
        private readonly Pipe[] nodePipes;

        public Pipeline(params AudioNode[] nodes)
        {
            if (nodes.Length == 0)
            {
                throw new System.ArgumentException("Pipeline must contain at least one node.");
            }
            this.nodes = nodes;
            nodePipes = new Pipe[nodes.Length];
            for (int i = 1; i < nodes.Length; i++)
            {
                nodePipes[i] = new Pipe(nodes[i - 1], nodes[i]);
            }
        }

        public override void Initialize(Context context)
        {
            foreach (var node in nodes)
            {
                node.Initialize(context);
            }
        }

        public override List<NamedValue> BuildInputs() => nodes[0].BuildInputs();

        public override List<NamedValue> BuildOutputs() => nodes[^1].BuildOutputs();

        public override AudioNode Clone()
        {
            AudioNode[] clonedNodes = new AudioNode[nodes.Length];
            for (int i = 0; i < nodes.Length; i++)
            {
                clonedNodes[i] = nodes[i].Clone();
            }
            return new Pipeline(clonedNodes);
        }

        public override void Process(Context context)
        {
            for (int i = 0; i < nodes.Length; i++)
            {
                if (i > 0)
                {
                    nodePipes[i - 1].Transfer();
                }
                nodes[i].Process(context);
            }
        }

        public override void ResetState()
        {
            foreach (var node in nodes)
            {
                node.ResetState();
            }
        }

        public override void Dispose()
        {
            foreach (var node in nodes)
            {
                node.Dispose();
            }
        }
    }
}