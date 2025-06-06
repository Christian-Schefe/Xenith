using DTO;
using ReactiveData.Core;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace ReactiveData.App
{
    public class ReactiveNode : IKeyed
    {
        public Reactive<Vector2> position;
        public Reactive<NodeResource> id;
        public ReactiveDict<string, ReactiveNodeSetting> settings;

        public ReactiveNode(Vector2 position, NodeResource id, Dictionary<string, ReactiveNodeSetting> settings)
        {
            this.position = new(position);
            this.id = new(id);
            this.settings = new(settings);
        }

        public string ID { get; private set; } = Guid.NewGuid().ToString();
        public string Key => ID;
    }
}
