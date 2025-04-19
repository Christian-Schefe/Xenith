using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace PianoRoll
{
    public class NoteEditor : MonoBehaviour
    {
        [SerializeField] private NoteRow rowPrefab;
        [SerializeField] private Subdivision subdivisionPrefab;
        [SerializeField] private Note notePrefab;
        [SerializeField] private SpriteRenderer selector;

        public Vector2 zoom = new(1, 1);

        private HashSet<Note> selectedNotes = new();

        private Dictionary<int, List<Note>> notes = new();

        private bool isDragging;
        private Dictionary<Note, Vector2> dragOffsets = new();
        private Note primaryDragNote;
        private bool isDraggingLengths;

        private bool isSelecting;
        private Vector2 selectionStart;

        private void Start()
        {
            for (int i = 0; i < 100; i++)
            {
                NoteRow row = Instantiate(rowPrefab, transform);
                row.Initialize(i);
            }
            for (int i = 0; i < 100; i++)
            {
                var subdivision = Instantiate(subdivisionPrefab, transform);
                subdivision.Initialize(i);
            }
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                Zoom(new(1.1f, 1));
            }
            if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                Zoom(new(1f / 1.1f, 1));
            }
            if (Input.GetKeyDown(KeyCode.Alpha3))
            {
                Zoom(new(1, 1.1f));
            }
            if (Input.GetKeyDown(KeyCode.Alpha4))
            {
                Zoom(new(1, 1f / 1.1f));
            }

            var mousePiano = ScreenToPianoCoords(Input.mousePosition);

            if (Input.GetMouseButtonDown(0))
            {
                bool isShift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
                if (TryGetHoveredNote(out var note))
                {
                    if (!selectedNotes.Contains(note)) SelectNote(note, isShift);
                    StartDragging(mousePiano, note);
                }
                else
                {
                    if (!isShift)
                    {
                        ClearSelection();
                        StartSelecting(mousePiano);
                    }
                    else
                    {
                        AddNote(SnapX(mousePiano.x), Mathf.FloorToInt(mousePiano.y), 1f);
                        ClearSelection();
                    }
                }
            }

            if (isDragging && Input.GetMouseButton(0))
            {
                DoDrag(mousePiano);
            }
            else
            {
                isDragging = false;
            }


            if (isSelecting && Input.GetMouseButton(0))
            {
                DoSelecting(mousePiano);
            }
            else
            {
                isSelecting = false;
                selector.gameObject.SetActive(false);
            }


            if (Input.GetKeyDown(KeyCode.Delete))
            {
                foreach (var note in selectedNotes)
                {
                    DeleteNote(note);
                }
                selectedNotes.Clear();
            }
        }

        private void DoDrag(Vector2 mousePiano)
        {
            if (!isDraggingLengths)
            {
                var snappedX = SnapX(mousePiano.x + dragOffsets[primaryDragNote].x) - dragOffsets[primaryDragNote].x;
                mousePiano.x = snappedX;

                foreach (var note in selectedNotes)
                {
                    var offset = dragOffsets[note];
                    var pos = mousePiano + offset;
                    var yPos = Mathf.RoundToInt(pos.y);
                    SetNotePosition(note, pos.x, yPos);
                }
            }
            else
            {
                var snappedX = SnapX(mousePiano.x + dragOffsets[primaryDragNote].x) - dragOffsets[primaryDragNote].x;
                mousePiano.x = snappedX;
                var length = snappedX - primaryDragNote.Position.x;

                foreach (var note in selectedNotes)
                {
                    note.SetLength(length + dragOffsets[note].x);
                }
            }
        }

        private void StartDragging(Vector2 mousePiano, Note source)
        {
            isDragging = true;
            primaryDragNote = source;
            dragOffsets.Clear();
            var distToRight = Mathf.Abs(primaryDragNote.EndX - mousePiano.x);
            isDraggingLengths = distToRight < 0.2f;

            var primaryOffset = primaryDragNote.length - (mousePiano.x - primaryDragNote.Position.x);

            foreach (var note in selectedNotes)
            {
                if (!isDraggingLengths)
                {
                    dragOffsets[note] = note.Position - mousePiano;
                }
                else
                {
                    dragOffsets[note] = new(note.length - primaryDragNote.length + primaryOffset, 0);
                }
            }
        }

        private void StartSelecting(Vector2 mousePiano)
        {
            isSelecting = true;
            selectionStart = mousePiano;
        }

        private void DoSelecting(Vector2 mousePiano)
        {
            var min = Vector2.Min(selectionStart, mousePiano);
            var max = Vector2.Max(selectionStart, mousePiano);
            var rect = new Rect(min, max - min);
            selector.gameObject.SetActive(true);
            selector.transform.localPosition = rect.center;
            selector.transform.localScale = new Vector3(rect.width, rect.height, 1);

            ClearSelection();

            foreach (var row in notes)
            {
                var yMin = row.Key;
                var yMax = yMin + 1;
                if (yMin > rect.yMax || yMax < rect.yMin)
                {
                    continue;
                }

                foreach (var note in row.Value)
                {
                    var noteRect = note.Rect;
                    if (rect.Overlaps(noteRect))
                    {
                        SelectNote(note, true);
                    }
                }
            }
        }

        private float SnapX(float x)
        {
            var lowerSubdivision = Mathf.FloorToInt(x);
            var upperSubdivision = Mathf.CeilToInt(x);
            var lowerDistance = Mathf.Abs(x - lowerSubdivision);
            var upperDistance = Mathf.Abs(x - upperSubdivision);
            var snapX = lowerDistance < 0.2f ? lowerSubdivision : (upperDistance < 0.2f ? upperSubdivision : x);
            return snapX;
        }

        private void SetNotePosition(Note note, float x, int y)
        {
            if (y != note.yPos)
            {
                var oldRow = notes[note.yPos];
                oldRow.Remove(note);
                if (!notes.ContainsKey(y))
                {
                    notes[y] = new List<Note>();
                }
                notes[y].Add(note);
            }
            note.SetPosition(x, y);
        }

        private void SelectNote(Note note, bool keepPrevious)
        {
            if (!keepPrevious)
            {
                ClearSelection();
            }
            note.SetSelected(true);
            selectedNotes.Add(note);
        }

        private void ClearSelection()
        {
            foreach (var note in selectedNotes)
            {
                note.SetSelected(false);
            }
            selectedNotes.Clear();
        }

        private void DeleteNote(Note note)
        {
            var row = notes[note.yPos];
            row.Remove(note);
            Destroy(note.gameObject);
        }

        private bool TryGetHoveredNote(out Note note)
        {
            var pianoPos = ScreenToPianoCoords(Input.mousePosition);
            var yPos = Mathf.FloorToInt(pianoPos.y);
            if (!notes.TryGetValue(yPos, out var noteRow))
            {
                note = null;
                return false;
            }
            foreach (var n in noteRow)
            {
                if (n.IsWithin(pianoPos.x))
                {
                    note = n;
                    return true;
                }
            }
            note = null;
            return false;
        }

        private void AddNote(float xPos, int yPos, float length)
        {
            var note = Instantiate(notePrefab, transform);
            note.Initialize(xPos, yPos, length);
            if (!notes.ContainsKey(yPos))
            {
                notes[yPos] = new List<Note>();
            }
            notes[yPos].Add(note);
        }

        public void Zoom(Vector2 factor)
        {
            var cam = Globals<CameraController>.Instance;
            var camWorldSize = new Vector2(cam.Cam.orthographicSize * cam.Cam.aspect, cam.Cam.orthographicSize);
            var camBottomLeftWorld = (Vector2)cam.Cam.transform.position - camWorldSize;
            var camBottomLeftPiano = WorldToPianoCoords(camBottomLeftWorld);
            zoom *= factor;
            var pos = cam.Cam.transform.position;
            Vector3 newPos = PianoToWorldCoords(camBottomLeftPiano) + camWorldSize;
            newPos.z = pos.z;
            cam.Cam.transform.position = newPos;
        }

        public Vector2 ScreenToPianoCoords(Vector2 screen)
        {
            var cam = Globals<CameraController>.Instance;
            var world = cam.Cam.ScreenToWorldPoint(screen);
            return WorldToPianoCoords(world);
        }

        public Vector2 WorldToPianoCoords(Vector2 world)
        {
            return world / zoom;
        }

        public Vector2 PianoToWorldCoords(Vector2 piano)
        {
            return piano * zoom;
        }

        public SerializedPianoRoll Serialize()
        {
            var serialized = new SerializedPianoRoll() { notes = new() };
            foreach (var row in notes)
            {
                foreach (var note in row.Value)
                {
                    serialized.notes.Add(new SerializedNote()
                    {
                        x = note.Position.x,
                        y = note.yPos,
                        length = note.length
                    });
                }
            }
            return serialized;
        }

        public void Deserialize(SerializedPianoRoll data)
        {
            foreach (var note in data.notes)
            {
                AddNote(note.x, note.y, note.length);
            }
        }
    }

    public struct SerializedPianoRoll
    {
        public List<SerializedNote> notes;

        public List<DSP.SequencerNote> GetNotes()
        {
            return notes.Select(note =>
            {
                var pitch = 110 * Mathf.Pow(2, note.y / 12f);
                return new DSP.SequencerNote(pitch, note.x, note.length);
            }).OrderBy(n => n.time).ToList();
        }
    }

    public struct SerializedNote
    {
        public float x;
        public int y;
        public float length;
    }
}
