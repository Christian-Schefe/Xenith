using System.Collections.Generic;
using UnityEngine;

namespace PianoRoll
{
    public class PianoRollVisuals : MonoBehaviour
    {
        [SerializeField] private Transform parent;

        [SerializeField] private RectTransform leftBar;
        [SerializeField] private RectTransform topBar;
        [SerializeField] private PianoKey pianoKeyPrefab;
        [SerializeField] private TimeBarBar barPrefab;

        [SerializeField] private Subdivision subdivisionPrefab;
        [SerializeField] private NoteRow rowPrefab;

        private readonly List<TimeBarBar> bars = new();
        private readonly List<Subdivision> subdivisions = new();
        private readonly List<NoteRow> rows = new();
        private readonly List<PianoKey> pianoKeys = new();

        public int GetBarByIndex(int index)
        {
            var noteEditor = Globals<NoteEditor>.Instance;

            var rect = noteEditor.ViewRectPiano();
            var firstBar = noteEditor.GetBar(rect.min);
            return firstBar + index;
        }

        public void SetVisible(bool visible)
        {
            parent.gameObject.SetActive(visible);
            leftBar.gameObject.SetActive(visible);
            topBar.gameObject.SetActive(visible);
        }

        public int GetSubdivisionByIndex(int index)
        {
            return GetBarByIndex(index / 4) * 4 + index % 4;
        }

        public int GetRowByOffset(int index)
        {
            var noteEditor = Globals<NoteEditor>.Instance;

            var rect = noteEditor.ViewRectPiano();
            var firstRow = noteEditor.GetRow(rect.min);
            return firstRow + index;
        }

        private void Update()
        {
            var noteEditor = Globals<NoteEditor>.Instance;

            var rect = noteEditor.ViewRectPiano();
            var firstBar = noteEditor.GetBar(rect.min);
            var lastBar = noteEditor.GetBar(rect.max) + 1;
            var barCount = lastBar - firstBar + 1;
            var subdivisionCount = barCount * 4;

            var firstRow = noteEditor.GetRow(rect.min);
            var lastRow = noteEditor.GetRow(rect.max) + 1;
            var rowCount = lastRow - firstRow + 1;

            AdjustCount(barPrefab, bars, barCount, (bar, i) =>
            {
                bar.Initialize(i);
            }, parent: topBar.transform);

            AdjustCount(subdivisionPrefab, subdivisions, subdivisionCount, (subdivision, i) =>
            {
                subdivision.Initialize(i);
            });

            AdjustCount(rowPrefab, rows, rowCount, (row, i) =>
            {
                row.Initialize(i);
            });

            AdjustCount(pianoKeyPrefab, pianoKeys, rowCount, (pianoKey, i) =>
            {
                pianoKey.Initialize(i);
            }, parent: leftBar.transform);
        }

        private void AdjustCount<T>(T prefab, List<T> list, int count, System.Action<T, int> onInstantiate, Transform parent = null) where T : MonoBehaviour
        {
            if (list.Count < count)
            {
                for (var i = list.Count; i < count; i++)
                {
                    var p = parent != null ? parent : this.parent;
                    var instance = Instantiate(prefab, p);
                    onInstantiate(instance, i);
                    list.Add(instance);
                }
            }
            else if (list.Count > count)
            {
                for (var i = list.Count - 1; i >= count; i--)
                {
                    Destroy(list[i].gameObject);
                    list.RemoveAt(i);
                }
            }
        }
    }
}
