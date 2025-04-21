using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


namespace NodeGraph
{
    public class NewDialog : BaseDialog
    {
        [SerializeField] private TMPro.TMP_InputField displayName;
        [SerializeField] private TMPro.TMP_InputField id;
        [SerializeField] private Button createButton;

        public override void Open()
        {
            base.Open();
            createButton.onClick.AddListener(OnCreate);
            id.text = "";
            displayName.text = "";
        }

        public override void Close()
        {
            base.Close();
            createButton.onClick.RemoveListener(OnCreate);
            id.text = "";
            displayName.text = "";
        }

        private void OnCreate()
        {
            var graphEditor = Globals<GraphEditor>.Instance;
            graphEditor.NewGraph(new(displayName.text, id.text, false));
            Close();
        }
    }
}

