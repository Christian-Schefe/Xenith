using System.Collections.Generic;
using UnityEngine;

namespace NodeGraph
{
    public class AddNodeDialog : MonoBehaviour
    {
        [SerializeField] private RectTransform scrollContent;
        [SerializeField] private TMPro.TMP_InputField searchField;
        [SerializeField] private ClickableItem itemPrefab;

        private List<ClickableItem> entries = new();
        private Vector2 position;
        public bool isOpen;

        private void OnEnable()
        {
            searchField.onValueChanged.AddListener(OnSearchFieldChanged);
        }

        private void OnDisable()
        {
            searchField.onValueChanged.RemoveListener(OnSearchFieldChanged);
            DestroyEntries();
        }

        public void Open(Vector2 dialogPosition, Vector2 position)
        {
            isOpen = true;
            this.position = position;
            gameObject.SetActive(true);
            searchField.text = "";
            transform.localPosition = dialogPosition;
            DestroyEntries();
            SpawnEntries("");
        }

        public void Close()
        {
            if (!isOpen) return;
            isOpen = false;
            DestroyEntries();
            gameObject.SetActive(false);
        }

        private void DestroyEntries()
        {
            if (entries.Count > 0)
            {
                foreach (var entry in entries)
                {
                    Destroy(entry.gameObject);
                }
                entries.Clear();
            }
        }

        private void SpawnEntries(string searchText)
        {
            var graphEditor = Globals<GraphEditor>.Instance;
            var nodeTypes = graphEditor.GetPlaceableNodes();
            foreach (var nodeType in nodeTypes)
            {
                if (searchText == "" || nodeType.displayName.ToLower().Contains(searchText.ToLower()))
                {
                    var entry = Instantiate(itemPrefab, scrollContent);
                    entry.SetText(nodeType.displayName);
                    entry.SetOnClickListener(() =>
                    {
                        graphEditor.Graph.AddNode(position, nodeType);
                        Close();
                    });
                    entries.Add(entry);
                }
            }
        }

        private void OnSearchFieldChanged(string searchText)
        {
            DestroyEntries();
            SpawnEntries(searchText);
        }
    }
}
