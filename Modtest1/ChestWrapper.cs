using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.GlobalIllumination;

namespace MuckModTest {
    [Serializable]
    public class ChestWrapper {
        public int id { get; }
        public List<String> items { get; }
        
        public Transform transform;

        public ChestWrapper(Transform transform, int id) {
            this.id = id;
            this.transform = transform;
            items = new List<string>();
        }
        
        public void addItem(String itemName) {
            items.Add(itemName);
        }
    }
}