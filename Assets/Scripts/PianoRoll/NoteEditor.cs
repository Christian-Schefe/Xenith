using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace PianoRoll
{
    public class NoteEditor : MonoBehaviour
    {
        [SerializeField] private RectTransform viewFrame;
        [SerializeField] private NoteRow rowPrefab;
        [SerializeField] private Subdivision subdivisionPrefab;
        [SerializeField] private Note notePrefab;
        [SerializeField] private SpriteRenderer selector;

        private Vector2 zoom = new(1f, 0.33f);
        public Vector2 Zoom => zoom;

        private HashSet<Note> selectedNotes = new();

        private Dictionary<int, List<Note>> notes = new();

        private bool isDragging;
        private Dictionary<Note, Vector2> dragOffsets = new();
        private Note primaryDragNote;
        private bool isDraggingLengths;

        private bool isSelecting;
        private Vector2 selectionStart;

        private bool isDraggingPlayPosition;

        public bool isPlaying;
        private float startPlayPosition;
        private float startPlayPlayTime;
        private float startPlayTime;

        private int edo = 12;
        public int Edo => edo;

        private int activeTrackIndex;

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
                DoZoom(new(1.1f, 1));
            }
            if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                DoZoom(new(1f / 1.1f, 1));
            }
            if (Input.GetKeyDown(KeyCode.Alpha3))
            {
                DoZoom(new(1, 1.1f));
            }
            if (Input.GetKeyDown(KeyCode.Alpha4))
            {
                DoZoom(new(1, 1f / 1.1f));
            }

            if (isPlaying)
            {
                var trackEditor = Globals<TrackEditor>.Instance;
                var timePlaying = Time.time - startPlayTime;
                var playPosition = Globals<PlayPosition>.Instance;
                var playTime = startPlayPlayTime + timePlaying;
                var playBeat = TempoController.GetBeatFromTime(playTime, trackEditor.tempoEvents);
                playPosition.SetPosition(playBeat);

                return;
            }

            var mousePiano = ScreenToPianoCoords(Input.mousePosition);

            if (Input.GetMouseButtonDown(0))
            {
                HandleMouseDown(mousePiano);
            }

            HandleDrag(mousePiano);

            if (Input.GetKeyDown(KeyCode.Delete))
            {
                foreach (var note in selectedNotes)
                {
                    DeleteNote(note);
                }
                selectedNotes.Clear();
            }

            if (Input.GetKeyDown(KeyCode.D))
            {
                var minStartTime = selectedNotes.Min(n => n.Position.x);
                var playPosition = Globals<PlayPosition>.Instance;
                foreach (var note in selectedNotes)
                {
                    var offset = note.Position.x - minStartTime;
                    AddNote(playPosition.position + offset, note.yPos, note.length);
                }
                ClearSelection();
            }

            var isShift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            if (Input.GetKeyDown(KeyCode.UpArrow))
            {
                MoveSelectedNotes(isShift ? edo : 1);
            }
            if (Input.GetKeyDown(KeyCode.DownArrow))
            {
                MoveSelectedNotes(isShift ? -edo : -1);
            }
        }

        public void SetActiveTrack(int newActiveTrack)
        {
            SaveNotesToTrack();
            ClearAll();
            activeTrackIndex = newActiveTrack;
            var trackEditor = Globals<TrackEditor>.Instance;
            var track = trackEditor.GetTrack(activeTrackIndex);
            Deserialize(track.PianoRoll);
        }

        private void MoveSelectedNotes(int deltaY)
        {
            foreach (var note in selectedNotes)
            {
                var newY = note.yPos + deltaY;
                SetNotePosition(note, note.Position.x, newY);
            }
        }

        public bool IsSpecialRow(int y)
        {
            return y % edo == 0;
        }

        public void StartPlaying()
        {
            if (isPlaying) return;
            isPlaying = true;
            SaveNotesToTrack();
            var playPosition = Globals<PlayPosition>.Instance;
            startPlayPosition = playPosition.position;
            startPlayTime = Time.time;
            startPlayPlayTime = GetPlayStartTime();

            isDragging = false;
            isSelecting = false;
            selector.gameObject.SetActive(false);
            isDraggingPlayPosition = false;
            ClearSelection();
        }

        public float GetPlayStartTime()
        {
            var trackEditor = Globals<TrackEditor>.Instance;
            var playPosition = Globals<PlayPosition>.Instance;
            var startTime = TempoController.GetTimeFromBeat(playPosition.position, trackEditor.tempoEvents);
            return startTime;
        }

        public void StopPlaying()
        {
            isPlaying = false;
            var playPosition = Globals<PlayPosition>.Instance;
            playPosition.SetPosition(startPlayPosition);
        }

        private void HandleDrag(Vector2 mousePiano)
        {
            if (Input.GetMouseButton(0) && isDragging)
            {
                DoDrag(mousePiano);
            }
            else
            {
                isDragging = false;
            }


            if (Input.GetMouseButton(0) && isSelecting)
            {
                DoSelecting(mousePiano);
            }
            else
            {
                isSelecting = false;
                selector.gameObject.SetActive(false);
            }

            if (Input.GetMouseButton(0) && isDraggingPlayPosition)
            {
                var playPosition = Globals<PlayPosition>.Instance;
                playPosition.SetPosition(SnapX(mousePiano.x));
            }
            else
            {
                isDraggingPlayPosition = false;
            }
        }

        private void HandleMouseDown(Vector2 mousePiano)
        {
            var viewRectScreen = ViewRectScreen();
            if (!viewRectScreen.Contains(Input.mousePosition))
            {
                return;
            }

            bool isShift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            if (!isShift && IsHoveringPlayPosition(mousePiano))
            {
                isDraggingPlayPosition = true;
                return;
            }

            if (TryGetHoveredNote(out var note))
            {
                if (!selectedNotes.Contains(note)) SelectNote(note, isShift);
                StartDragging(mousePiano, note);
                return;
            }

            if (!isShift)
            {
                ClearSelection();
                StartSelecting(mousePiano);
            }
            else
            {
                AddNote(Mathf.FloorToInt(mousePiano.x), Mathf.FloorToInt(mousePiano.y), 1f);
                ClearSelection();
            }
        }

        private bool IsHoveringPlayPosition(Vector2 mousePiano)
        {
            var playPosition = Globals<PlayPosition>.Instance;
            var xDist = Mathf.Abs(mousePiano.x - playPosition.position);
            var camWorldY = ViewRectWorld().yMax;
            var mouseWorldY = PianoToWorldCoords(mousePiano).y;
            var yDist = camWorldY - mouseWorldY;
            return xDist < 0.2f || (yDist < 1f && yDist >= 0);
        }

        public Rect CamPianoRect()
        {
            var cam = Globals<CameraController>.Instance.Cam;
            var camHalfSize = new Vector2(cam.orthographicSize * cam.aspect, cam.orthographicSize);
            var camBottomLeft = WorldToPianoCoords((Vector2)cam.transform.position - camHalfSize);
            var camTopRight = WorldToPianoCoords((Vector2)cam.transform.position + camHalfSize);
            return new Rect(camBottomLeft, camTopRight - camBottomLeft);
        }

        public Rect ViewRectWorld()
        {
            var screenRect = ViewRectScreen();
            var minWorld = PianoToWorldCoords(ScreenToPianoCoords(screenRect.min));
            var maxWorld = PianoToWorldCoords(ScreenToPianoCoords(screenRect.max));
            return Rect.MinMaxRect(minWorld.x, minWorld.y, maxWorld.x, maxWorld.y);
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
            selector.transform.localPosition = PianoToWorldCoords(rect.center);
            selector.transform.localScale = new Vector3(rect.width * zoom.x, rect.height * zoom.y, 1);

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
            x = Mathf.Max(x, 0);
            y = Mathf.Max(y, 0);

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

        public void DoZoom(Vector2 factor)
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

        public Rect ViewRectScreen()
        {
            Vector3[] corners = new Vector3[4];
            viewFrame.GetWorldCorners(corners);

            // Calculate the screen space rectangle
            float minX = Mathf.Min(corners[0].x, corners[1].x, corners[2].x, corners[3].x);
            float minY = Mathf.Min(corners[0].y, corners[1].y, corners[2].y, corners[3].y);
            float width = Mathf.Max(corners[0].x, corners[1].x, corners[2].x, corners[3].x) - minX;
            float height = Mathf.Max(corners[0].y, corners[1].y, corners[2].y, corners[3].y) - minY;

            // Display the screen space rectangle
            Rect screenRect = new(minX, minY, width, height);
            return screenRect;
        }

        public Vector2 WorldToPianoCoords(Vector2 world)
        {
            return world / Zoom;
        }

        public Vector2 PianoToWorldCoords(Vector2 piano)
        {
            return piano * Zoom;
        }

        public int GetBar(Vector2 piano)
        {
            return Mathf.FloorToInt(piano.x / 4f);
        }

        public void SaveNotesToTrack()
        {
            var trackEditor = Globals<TrackEditor>.Instance;
            trackEditor.GetTrack(activeTrackIndex).PianoRoll = Serialize();
        }

        public void ClearAll()
        {
            foreach (var row in notes)
            {
                foreach (var note in row.Value)
                {
                    Destroy(note.gameObject);
                }
            }
            notes.Clear();
            selectedNotes.Clear();
            isDragging = false;
            isSelecting = false;
            isDraggingPlayPosition = false;
            selector.gameObject.SetActive(false);
            dragOffsets.Clear();
            primaryDragNote = null;
            isDraggingLengths = false;
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

        public readonly List<DSP.SequencerNote> GetNotes(List<TempoEvent> tempoEvents)
        {
            var noteEditor = Globals<NoteEditor>.Instance;
            var unsortedTempoNotes = notes.Select(note =>
            {
                var pitch = 110 * Mathf.Pow(2, (float)note.y / noteEditor.Edo);
                Debug.Log($"{note.y}, {pitch}");
                return new TempoNote(note.x, note.length, pitch);
            }).ToList();
            var sequencerNotes = TempoController.ConvertNotes(unsortedTempoNotes, tempoEvents);
            return sequencerNotes;
        }
    }

    public struct SerializedNote
    {
        public float x;
        public int y;
        public float length;
    }
}
