using System.Collections.Generic;
using UnityEngine;

namespace NodeGraph
{
    public class OpenDialog : BaseDialog
    {
        [SerializeField] private TMPro.TMP_InputField search;
        [SerializeField] private RectTransform scrollContent;
        [SerializeField] private ClickableItem itemPrefab;

        private readonly List<ClickableItem> items = new();
        private IEnumerable<NodeResource> graphs;

        public void SetGraphs(IEnumerable<NodeResource> graphs)
        {
            this.graphs = graphs;
        }

        public override void Open()
        {
            base.Open();
            CreateItems();
            search.onValueChanged.AddListener(HideItems);
        }

        public override void Close()
        {
            base.Close();
            ClearItems();
            search.onValueChanged.RemoveListener(HideItems);
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Close();
            }
        }

        private void ClearItems()
        {
            foreach (var item in items)
            {
                Destroy(item.gameObject);
            }
            items.Clear();
        }

        private void HideItems(string searchText)
        {
            foreach (var item in items)
            {
                var text = item.GetComponentInChildren<TMPro.TMP_Text>().text.ToLower();
                item.gameObject.SetActive(searchText == "" || text.Contains(searchText));
            }
        }

        private void CreateItems()
        {
            var graphEditor = Globals<GraphEditor>.Instance;
            ClearItems();
            foreach (var graph in graphs)
            {
                var item = Instantiate(itemPrefab, scrollContent);
                items.Add(item);
                item.SetText(graph.displayName);
                item.SetOnClickListener(() =>
                {
                    graphEditor.OpenGraph(graph);
                    Close();
                });
            }
            var searchText = search.text.ToLower();
            HideItems(searchText);
        }
    }
}
