using DTO;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace PianoRoll
{
    public class NoteEditor : MonoBehaviour
    {
        [SerializeField] private RectTransform canvasRT;
        [SerializeField] private RectTransform viewFrame;
        [SerializeField] private Note notePrefab;
        [SerializeField] private SpriteRenderer selector;

        private Vector2 zoom = new(1f, 0.33f);
        public Vector2 Zoom => zoom;

        private readonly HashSet<Note> selectedNotes = new();
        private readonly Dictionary<int, List<Note>> notes = new();
        private readonly Dictionary<Note, Vector2> dragOffsets = new();

        private bool isDragging;
        private Note primaryDragNote;
        private bool isDraggingLengths;

        private bool isSelecting;
        private Vector2 selectionStart;

        private bool isDraggingPlayPosition;

        public bool isPlaying;
        private float startPlayPosition;
        private float startPlayPlayTime;
        private float startPlayTime;

        private Song activeSong = null;
        private DTO.Track activeTrack = null;
        public DTO.Track ActiveTrack => activeTrack;

        private MusicKey musicKey = MusicKey.CMajor;
        public MusicKey Key => musicKey;

        public List<int> stepsList;
        private Dictionary<int, int> pianoStepsMap;

        private float lastSelectedLength = 1f;


        private void Start()
        {
            BuildStepsList();
        }

        private void BuildStepsList()
        {
            stepsList = new List<int>();
            int octaves = 12;

            for (int i = 0; i < octaves; i++)
            {
                int stepOffset = i * musicKey.edo;
                for (int j = 0; j < musicKey.pitches.Count; j++)
                {
                    int steps = musicKey.pitches[j] + stepOffset;
                    stepsList.Add(steps);
                }
            }

            var accidentalRows = new List<int>();
            foreach (var row in notes)
            {
                if (!stepsList.Contains(row.Key) && row.Value.Count > 0)
                {
                    accidentalRows.Add(row.Key);
                }
            }

            foreach (var accidental in accidentalRows)
            {
                stepsList.Add(accidental);
            }
            stepsList.Sort();

            pianoStepsMap = new Dictionary<int, int>();
            for (int i = 0; i < stepsList.Count; i++)
            {
                pianoStepsMap[stepsList[i]] = i;
            }
        }

        private void Update()
        {
            if (activeSong == null) return;

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
                var timePlaying = Globals<DSP.DSP>.Instance.playTime.value;
                var playPosition = Globals<PlayPosition>.Instance;
                var playTime = startPlayPlayTime + timePlaying;
                var playBeat = TempoController.GetBeatFromTime(playTime, activeSong.tempoEvents);
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

            if (Input.GetKeyDown(KeyCode.D) && selectedNotes.Count > 0)
            {
                var minStartTime = selectedNotes.Min(n => n.Position.x);
                var playPosition = Globals<PlayPosition>.Instance;
                foreach (var note in selectedNotes)
                {
                    var offset = note.Position.x - minStartTime;
                    AddNote(playPosition.position + offset, note.GetYPos(), note.Length);
                }
            }

            var isShift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            if (Input.GetKeyDown(KeyCode.UpArrow))
            {
                MoveSelectedNotes(isShift ? musicKey.edo : 1);
            }
            if (Input.GetKeyDown(KeyCode.DownArrow))
            {
                MoveSelectedNotes(isShift ? -musicKey.edo : -1);
            }
        }

        public void SetSong(Song song)
        {
            activeSong = song;
        }

        public void ShowTrack(DTO.Track newActiveTrack)
        {
            ClearAll();
            activeTrack = newActiveTrack;
            var camera = Globals<CameraController>.Instance;
            camera.ResetPosition();

            LoadTrack();
        }

        public void HideTrack()
        {
            activeSong = null;
            activeTrack = null;
            ClearAll();
        }

        public void LoadTrack()
        {
            foreach (var noteData in activeTrack.notes)
            {
                var note = Instantiate(notePrefab, transform);
                note.Initialize(noteData);
                if (!notes.ContainsKey(noteData.y))
                {
                    notes[noteData.y] = new List<Note>();
                }
                notes[noteData.y].Add(note);
            }
            BuildStepsList();
        }

        private void MoveSelectedNotes(int deltaY)
        {
            foreach (var note in selectedNotes)
            {
                SetNoteSteps(note, note.Y + deltaY);
            }
            BuildStepsList();
        }

        public bool IsSpecialRow(int steps)
        {
            return steps % musicKey.edo == 0;
        }

        public bool IsAccidentalRow(int steps)
        {
            return !musicKey.pitches.Contains(steps % musicKey.edo);
        }

        public void StartPlaying()
        {
            if (isPlaying) return;
            isPlaying = true;
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
            var playPosition = Globals<PlayPosition>.Instance;
            var startTime = TempoController.GetTimeFromBeat(playPosition.position, activeSong.tempoEvents);
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

        private bool IsMouseOverUI()
        {
            return UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject();
        }

        private void HandleMouseDown(Vector2 mousePiano)
        {
            var viewRectScreen = ViewRectScreen();
            if (!viewRectScreen.Contains(Input.mousePosition))
            {
                return;
            }

            if (IsMouseOverUI())
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
                lastSelectedLength = note.Length;
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
                AddNote(Mathf.FloorToInt(mousePiano.x), Mathf.FloorToInt(mousePiano.y), lastSelectedLength);
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

        public Rect ViewRectWorld()
        {
            var screenRect = ViewRectScreen();
            var minWorld = PianoToWorldCoords(ScreenToPianoCoords(screenRect.min));
            var maxWorld = PianoToWorldCoords(ScreenToPianoCoords(screenRect.max));
            return Rect.MinMaxRect(minWorld.x, minWorld.y, maxWorld.x, maxWorld.y);
        }

        public Rect ViewRectPiano()
        {
            var screenRect = ViewRectScreen();
            var minPiano = ScreenToPianoCoords(screenRect.min);
            var maxPiano = ScreenToPianoCoords(screenRect.max);
            return Rect.MinMaxRect(minPiano.x, minPiano.y, maxPiano.x, maxPiano.y);
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
                BuildStepsList();
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
                lastSelectedLength = primaryDragNote.Length;
            }
        }

        private void StartDragging(Vector2 mousePiano, Note source)
        {
            isDragging = true;
            primaryDragNote = source;
            dragOffsets.Clear();
            var distToRight = Mathf.Abs(primaryDragNote.EndX - mousePiano.x);
            isDraggingLengths = distToRight < 0.2f;

            var primaryOffset = primaryDragNote.Length - (mousePiano.x - primaryDragNote.Position.x);

            foreach (var note in selectedNotes)
            {
                if (!isDraggingLengths)
                {
                    dragOffsets[note] = note.Position - mousePiano;
                }
                else
                {
                    dragOffsets[note] = new(note.Length - primaryDragNote.Length + primaryOffset, 0);
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
                var yMin = StepsToPiano(row.Key);
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

            var steps = PianoToSteps(y);

            if (steps != note.Y)
            {
                var oldRow = notes[note.Y];
                oldRow.Remove(note);
                if (!notes.ContainsKey(steps))
                {
                    notes[steps] = new List<Note>();
                }
                notes[steps].Add(note);
                if (oldRow.Count == 0)
                {
                    notes.Remove(note.Y);
                }
            }
            note.SetPosition(x, steps);
        }


        private void SetNoteSteps(Note note, int steps)
        {
            int ySteps = Mathf.Max(steps, 0);

            if (ySteps != note.Y)
            {
                var oldRow = notes[note.Y];
                oldRow.Remove(note);
                if (!notes.ContainsKey(ySteps))
                {
                    notes[ySteps] = new List<Note>();
                }
                notes[ySteps].Add(note);
                if (oldRow.Count == 0)
                {
                    notes.Remove(note.Y);
                }
            }
            note.SetPosition(note.X, ySteps);
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
            var row = notes[note.Y];
            row.Remove(note);
            activeTrack.notes.Remove(note.note);
            Destroy(note.gameObject);
            if (row.Count == 0)
            {
                notes.Remove(note.Y);
            }
        }

        private bool TryGetHoveredNote(out Note note)
        {
            var pianoPos = ScreenToPianoCoords(Input.mousePosition);
            var yPos = PianoToSteps(Mathf.FloorToInt(pianoPos.y));
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
            var noteGO = Instantiate(notePrefab, transform);
            var clampedX = Mathf.Max(xPos, 0);
            var clampedY = Mathf.Max(yPos, 0);
            var yIndex = PianoToSteps(clampedY);
            var note = new DTO.Note(clampedX, yIndex, length);
            activeTrack.notes.Add(note);
            noteGO.Initialize(note);
            if (!notes.ContainsKey(yIndex))
            {
                notes[yIndex] = new List<Note>();
            }
            notes[yIndex].Add(noteGO);
        }

        public int StepsToPiano(int steps)
        {
            return pianoStepsMap.TryGetValue(steps, out var piano) ? piano : 0;
        }

        public int PianoToSteps(float piano)
        {
            var m = stepsList.Count;
            return stepsList[((Mathf.FloorToInt(piano) % m) + m) % m];
        }

        public void DoZoom(Vector2 factor)
        {
            var pianoRect = ViewRectPiano();
            zoom *= factor;
            var cam = Globals<CameraController>.Instance;
            var worldRect = ViewRectWorld();
            Vector3 offset = worldRect.min - PianoToWorldCoords(pianoRect.min);
            cam.Cam.transform.position -= offset;
        }

        public Vector2 ScreenToCanvasCoords(Vector2 screen)
        {
            var cam = Globals<CameraController>.Instance.Cam;
            var canvasRect = canvasRT.rect;
            var viewportPoint = cam.ScreenToViewportPoint(screen);
            var canvasPos = canvasRect.size * viewportPoint;
            return canvasPos;
        }

        public Vector2 ScreenToPianoCoords(Vector2 screen)
        {
            var cam = Globals<CameraController>.Instance;
            var world = cam.Cam.ScreenToWorldPoint(screen);
            return WorldToPianoCoords(world);
        }

        public Vector2 PianoToScreenCoords(Vector2 piano)
        {
            var cam = Globals<CameraController>.Instance;
            var world = PianoToWorldCoords(piano);
            return cam.Cam.WorldToScreenPoint(world);
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

        public int GetRow(Vector2 piano)
        {
            return Mathf.FloorToInt(piano.y);
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
    }
}
