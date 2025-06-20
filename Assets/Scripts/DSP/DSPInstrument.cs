using System.Linq;

namespace DSP
{
    public struct DSPInstrument
    {
        public AudioNode instrument;
        public AudioNode effects;

        public DSPInstrument(AudioNode instrument, AudioNode effects)
        {
            this.instrument = instrument;
            this.effects = effects;
        }

        public readonly void Validate()
        {
            ValidateInstrument();
            ValidateEffect();
        }

        private readonly void ValidateInstrument()
        {
            var instrumentInput = instrument.BuildInputs();
            var instrumentOutput = instrument.BuildOutputs();

            if (instrumentInput.Count != 0)
            {
                throw new System.Exception("Instrument must have zero inputs.");
            }
            if (instrumentOutput.Count != 2)
            {
                throw new System.Exception("Instrument must have two output.");
            }
            if (instrumentOutput.Any(e => e.Value is not FloatValue))
            {
                throw new System.Exception("Instrument output must be FloatValue.");
            }
        }

        private readonly void ValidateEffect()
        {
            var effectsInput = effects.BuildInputs();
            var effectsOutput = effects.BuildOutputs();

            if (effectsInput.Count != 2)
            {
                throw new System.Exception("Effects must have two inputs.");
            }
            if (effectsInput.Any(e => e.Value is not FloatValue))
            {
                throw new System.Exception("Effects input must be FloatValue.");
            }
            if (effectsOutput.Count != 2)
            {
                throw new System.Exception("Effects must have two outputs.");
            }
            if (effectsOutput.Any(e => e.Value is not FloatValue))
            {
                throw new System.Exception("Effects output must be FloatValue.");
            }
        }
    }
    public struct DSPMaster
    {
        public AudioNode effects;

        public DSPMaster(AudioNode effects)
        {
            this.effects = effects;
        }

        public readonly void Validate()
        {
            var effectsInput = effects.BuildInputs();
            var effectsOutput = effects.BuildOutputs();

            if (effectsInput.Count != 2)
            {
                throw new System.Exception("Master must have two inputs.");
            }
            if (effectsInput.Any(e => e.Value is not FloatValue))
            {
                throw new System.Exception("Master input must be FloatValue.");
            }
            if (effectsOutput.Count != 2)
            {
                throw new System.Exception("Master must have two outputs.");
            }
            if (effectsOutput.Any(e => e.Value is not FloatValue))
            {
                throw new System.Exception("Master output must be FloatValue.");
            }
        }
    }
}