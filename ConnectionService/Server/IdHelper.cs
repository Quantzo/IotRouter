using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConnectionService.Server
{
    class IdHelper
    {
        private Dictionary<string, int> _dictionary;
        private int _maxNumOfBindings;
        private List<int> _valuesInts;

        public IdHelper(int numberOfBindings)
        {
            _dictionary = new Dictionary<string, int>();
            _valuesInts = Enumerable.Range(1, numberOfBindings).ToList();
            _maxNumOfBindings = numberOfBindings;
        }
        public bool IsBindingPosible() => _valuesInts.Count != 0;
        public bool IsBinded(string guid) => _dictionary.ContainsKey(guid);

        public void Bind(string guid)
        {
            _dictionary.Add(guid, GenerateNewValue());
        }
        public void UnBind(string guid)
        {
            var x = _dictionary.First(s => s.Key == guid);
            RetrunValueToPool(x.Value);
            _dictionary.Remove(guid);
        }

        public int CheckBinding(string guid) => _dictionary.First(s => s.Key == guid).Value;


        private int GenerateNewValue()
        {
            var x = _valuesInts.First();
            _valuesInts.Remove(x);
            return x;
        }

        private void RetrunValueToPool(int value)
        {
            _valuesInts.Add(value);
        }
    }
}
