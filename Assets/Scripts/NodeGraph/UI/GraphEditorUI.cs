using ActionMenu;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace NodeGraph
{
    public class GraphEditorUI : MonoBehaviour
    {
        [SerializeField] private OpenDialog openDialog;
        [SerializeField] private NewDialog newDialog;
        [SerializeField] private GameObject overlay;

        private BaseDialog currentDialog;

        private void Awake()
        {
            var actionBar = Globals<ActionBar>.Instance;
            actionBar.AddActions(new()
            {
                new TopLevelAction("File", new()
                {
                    new ActionType.Button("New", OnClickNew),
                    new ActionType.Button("Open", OnClickOpen),
                    new ActionType.Button("Save", OnClickSave),
                    new ActionType.Button("Delete", OnClickDelete),
                })
            });
        }

        private void Update()
        {
            var graphEditor = Globals<GraphEditor>.Instance;
            var actionBar = Globals<ActionBar>.Instance;
            actionBar.SetTitle(graphEditor.TryGetGraphDisplayName(out var name) ? name : "No graph loaded");

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
