using DTO;
using ReactiveData.App;
using ReactiveData.Core;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace PianoRoll
{
    public class NoteEditor : MonoBehaviour
    {
        [SerializeField] private RectTransform canvasRT;
        [SerializeField] private RectTransform viewFrame;
        [SerializeField] private RectTransform tempoMarkerParent;
        [SerializeField] private Note notePrefab, bgNotePrefab;
        [SerializeField] private TempoMarker tempoMarkerPrefab;
        [SerializeField] private SpriteRenderer selector;

        public Reactive<Vector2> zoom = new(new(1f, 0.33f));
        public Vector2 Zoom => zoom.Value;

        private float xSnap = 2f;

        public readonly ReactiveHashSet<Note> selectedNotes = new();
        private readonly Dictionary<Note, Vector2> dragOffsets = new();
        public readonly Reactive<ReactiveTempoEvent> selectedEvent = new(null);

        private bool isDragging;
        private Note primaryDragNote;
        private bool isDraggingLengths;

        private bool isSelecting;
        private Vector2 selectionStart;

        private bool isDraggingPlayPosition;

        public bool isPlaying;
        private float startPlayPosition;
        private float startPlayPlayTime;

        public ReactiveSong activeSong;

        public ReactiveList<int> stepsList = new();
        private Dictionary<int, int> pianoStepsMap;

        private float lastSelectedLength = 1f;

        public MusicKey Key => activeSong?.activeTrack.Value.keySignature.Value ?? MusicKey.CMajor;

        private ReactiveChainedEnumerable<ReactiveNote> bgNotes = new();
        private ReactiveUIBinder<ReactiveTrack, TrackObserver> trackBinder = null;
        private ReactiveUIBinder<ReactiveNote, Note> bgNoteBinder = null;
        private ReactiveUIBinder<ReactiveNote, Note> noteBinder = null;
        private ReactiveUIBinder<ReactiveTempoEvent, TempoMarker> tempoEventBinder = null;

        private void Awake()
        {
            var main = Globals<Main>.Instance;
            main.app.openElement.AddAndCall(OnOpenElementChanged);
        }

        private void Start()
        {
            BuildStepsList();
        }

        private void BuildStepsList()
        {
            var newStepsList = new List<int>();
            pianoStepsMap = new Dictionary<int, int>();
            int octaves = 12;

            if (activeSong != null)
            {
                for (int i = 0; i < octaves; i++)
                {
                    int stepOffset = i * Key.edo;
                    for (int j = 0; j < Key.pitches.Count; j++)
                    {
                        int steps = Key.pitches[j] + stepOffset;
                        newStepsList.Add(steps);
                    }
                }

                foreach (var note in activeSong.activeTrack.Value.notes)
                {
                    if (!newStepsList.Contains(note.pitch.Value))
                    {
                        newStepsList.Add(note.pitch.Value);
                    }
                }
                foreach (var note in bgNotes)
                {
                    if (!newStepsList.Contains(note.pitch.Value))
                    {
                        newStepsList.Add(note.pitch.Value);
                    }
                }
            }

            newStepsList.Sort();

            for (int i = 0; i < newStepsList.Count; i++)
            {
                pianoStepsMap[newStepsList[i]] = i;
            }

            stepsList.ReplaceAll(newStepsList);
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

                var cam = Globals<CameraController>.Instance;
                cam.SetCenterXPosition(playBeat);

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
                    activeSong.activeTrack.Value.notes.Remove(note.note);
                }
                selectedNotes.Clear();
                BuildStepsList();
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
                MoveSelectedNotes(isShift ? Key.edo : 1);
            }
            if (Input.GetKeyDown(KeyCode.DownArrow))
            {
                MoveSelectedNotes(isShift ? -Key.edo : -1);
            }
        }

        private void OnOpenElementChanged(Nand<ReactiveSong, ReactiveGraph> openElement)
        {
            bool visible = openElement.TryGet(out ReactiveSong song);
            InitializeBinders();
            var pianoRollVisuals = Globals<PianoRollVisuals>.Instance;
            pianoRollVisuals.SetVisible(visible);
            if (visible)
            {
                activeSong = song;
                trackBinder.ChangeSource(song.tracks);
                tempoEventBinder.ChangeSource(song.tempoEvents);
                song.activeTrack.AddAndCall(OnActiveTrackChanged);
            }
            else
            {
                activeSong?.activeTrack.Remove(OnActiveTrackChanged);
                activeSong = null;
                trackBinder.ChangeSource(null);
                noteBinder.ChangeSource(null);
                tempoEventBinder.ChangeSource(null);
                bgNotes.ClearSources();
                ResetState();
            }
        }

        private void InitializeBinders()
        {
            trackBinder ??= new(null, _ => new TrackObserver(ChangeBGNoteSources), _ => { });
            noteBinder ??= new(null, _ => Instantiate(notePrefab, transform), note => Destroy(note.gameObject));
            bgNoteBinder ??= new(bgNotes, _ => Instantiate(bgNotePrefab, transform), note => Destroy(note.gameObject));
            tempoEventBinder ??= new(null, _ => Instantiate(tempoMarkerPrefab, tempoMarkerParent), marker => Destroy(marker.gameObject));
        }

        private void OnActiveTrackChanged(ReactiveTrack newActiveTrack)
        {
            ResetState();
            InitializeBinders();
            noteBinder.ChangeSource(newActiveTrack.notes);
            ChangeBGNoteSources();
        }

        private void ChangeBGNoteSources()
        {
            bgNotes.ReplaceSources(activeSong.tracks.Where(t => t != activeSong.activeTrack.Value && t.isBGVisible.Value).Select(t => t.notes));
            BuildStepsList();
        }

        private void MoveSelectedNotes(int deltaY)
        {
            foreach (var note in selectedNotes)
            {
                SetNoteSteps(note, note.Pitch + deltaY);
            }
            BuildStepsList();
        }

        public bool IsSpecialRow(int steps)
        {
            return steps % Key.edo == Key.primaryPitch;
        }

        public bool IsAccidentalRow(int steps)
        {
            return !Key.pitches.Contains(steps % Key.edo);
        }

        public void StartPlaying()
        {
            if (isPlaying) return;
            isPlaying = true;
            var playPosition = Globals<PlayPosition>.Instance;
            startPlayPosition = playPosition.position;
            startPlayPlayTime = GetPlayStartTime();

            isDragging = false;
            isSelecting = false;
            selector.gameObject.SetActive(false);
            isDraggingPlayPosition = false;
            selectedNotes.Clear();
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
                lastSelectedLength = note.Length;
                if (!selectedNotes.Contains(note)) SelectNote(note, isShift);
                StartDragging(mousePiano, note);
                return;
            }

            if (!isShift)
            {
                selectedNotes.Clear();
                StartSelecting(mousePiano);
            }
            else
            {
                AddNote(SnapX(mousePiano.x, true), Mathf.FloorToInt(mousePiano.y), lastSelectedLength);
                selectedNotes.Clear();
            }
        }

        private bool IsHoveringPlayPosition(Vector2 mousePiano)
        {
            var playPosition = Globals<PlayPosition>.Instance;
            var xDist = Mathf.Abs(mousePiano.x - playPosition.position);
            float threshold = 0.2f / Zoom.x;
            return xDist < threshold;
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
                    note.note.length.Value = Mathf.Max(0.01f, length + dragOffsets[note].x);
                }
                lastSelectedLength = primaryDragNote.Length;
            }
        }

        private void StartDragging(Vector2 mousePiano, Note source)
        {
            isDragging = true;
            primaryDragNote = source;
            dragOffsets.Clear();
            var distToRight = Mathf.Abs(primaryDragNote.EndBeat - mousePiano.x);
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
            selector.transform.localScale = new Vector3(rect.width * Zoom.x, rect.height * Zoom.y, 1);

            selectedNotes.Clear();

            var newSelection = new List<Note>();

            foreach (var note in noteBinder.UIElements)
            {
                var noteRect = note.Rect;
                if (rect.Overlaps(noteRect))
                {
                    newSelection.Add(note);
                }
            }
            selectedNotes.AddRange(newSelection);
        }

        public float SnapX(float x, bool forceSnapDown = false)
        {
            var lowerSubdivision = Mathf.FloorToInt(x * xSnap) / xSnap;
            var upperSubdivision = Mathf.CeilToInt(x * xSnap) / xSnap;
            var lowerDistance = Mathf.Abs(x - lowerSubdivision);
            var upperDistance = Mathf.Abs(x - upperSubdivision);
            float threshold = 0.2f / Zoom.x;
            bool closeToLower = lowerDistance < threshold || forceSnapDown;
            bool closeToUpper = upperDistance < threshold && !forceSnapDown;
            return (closeToLower && closeToUpper) ? (lowerDistance < upperDistance ? lowerSubdivision : upperSubdivision)
                : (closeToLower ? lowerSubdivision : (closeToUpper ? upperSubdivision : x));
        }

        private void SetNotePosition(Note note, float x, int y)
        {
            note.note.beat.Value = Mathf.Max(x, 0);
            note.note.pitch.Value = PianoToSteps(Mathf.Max(y, 0));
        }

        private void SetNoteSteps(Note note, int steps)
        {
            note.note.pitch.Value = Mathf.Max(steps, 0);
        }

        private void SelectNote(Note note, bool keepPrevious)
        {
            if (!keepPrevious)
            {
                selectedNotes.Clear();
            }
            selectedNotes.Add(note);
        }

        private bool TryGetHoveredNote(out Note note)
        {
            var pianoPos = ScreenToPianoCoords(Input.mousePosition);
            var yPos = PianoToSteps(Mathf.FloorToInt(pianoPos.y));

            foreach (var n in noteBinder.UIElements)
            {
                if (n.Pitch == yPos && n.IsWithin(pianoPos.x))
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
            var clampedX = Mathf.Max(xPos, 0);
            var clampedY = Mathf.Max(yPos, 0);
            var yIndex = PianoToSteps(clampedY);
            var note = new ReactiveNote(clampedX, yIndex, 1.0f, length);
            activeSong.activeTrack.Value.notes.Add(note);
        }

        public int StepsToPiano(int steps)
        {
            return pianoStepsMap.TryGetValue(steps, out var piano) ? piano : 0;
        }

        public int PianoToSteps(float piano)
        {
            var m = stepsList.Count;
            if (m == 0) return 0;
            return stepsList[((Mathf.FloorToInt(piano) % m) + m) % m];
        }

        public void DoZoom(Vector2 factor)
        {
            var pianoRect = ViewRectPiano();
            zoom.Value *= factor;
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

        public void ResetState()
        {
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
