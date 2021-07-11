using System;
using System.Collections;
using System.Collections.Generic;

namespace MuckModTest {
    public class ChestWrapper {
        public int id { get; }
        public List<String> items { get; }
        public float x { get; }
        public float y { get; }

        public ChestWrapper(float x, float y, int id) {
            this.id = id;
            this.x = x;
            this.y = y;
            items = new List<string>();
        }
        
        public void addItem(String itemName) {
            items.Add(itemName);
        }
    }
}