using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace NodeGraph
{
    public class GraphEditorUI : MonoBehaviour
    {
        [SerializeField] private Button newButton, openButton, saveButton, deleteButton;
        [SerializeField] private OpenDialog openDialog;
        [SerializeField] private NewDialog newDialog;
        [SerializeField] private TMPro.TextMeshProUGUI title;
        [SerializeField] private GameObject overlay;

        private BaseDialog currentDialog;

        private void Awake()
        {
            newButton.onClick.AddListener(OnClickNew);
            openButton.onClick.AddListener(OnClickOpen);
            saveButton.onClick.AddListener(OnClickSave);
            deleteButton.onClick.AddListener(OnClickDelete);
        }

        private void Update()
        {
            var graphEditor = Globals<GraphEditor>.Instance;
            title.text = graphEditor.TryGetGraphDisplayName(out var name) ? name : "No graph loaded";

            overlay.SetActive(IsDialogOpen());

            if (Input.GetKeyDown(KeyCode.Escape) && IsDialogOpen())
            {
                currentDialog.Close();
                currentDialog = null;
            }
        }

        public bool IsDialogOpen()
        {
            return currentDialog != null && currentDialog.isOpen;
        }

        private void OnClickOpen()
        {
            var graphDatabase = Globals<GraphDatabase>.Instance;
            var graphs = graphDatabase.GetGraphs().Select(g => g.id);
            openDialog.SetGraphs(graphs);
            openDialog.Open();
            currentDialog = openDialog;
        }

        private void OnClickNew()
        {
            newDialog.Open();
            currentDialog = newDialog;
        }

        private void OnClickSave()
        {
            var graphEditor = Globals<GraphEditor>.Instance;
            graphEditor.SaveGraph();
        }

        private void OnClickDelete()
        {
            var graphEditor = Globals<GraphEditor>.Instance;
            graphEditor.DeleteGraph();
        }
    }
}
