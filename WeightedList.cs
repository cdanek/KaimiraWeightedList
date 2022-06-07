using System.Text;
using System;
using System.Collections;
using System.Collections.Generic;
using static KaimiraGames.WeightErrorHandlingType;

namespace KaimiraGames
{
    public class WeightedList<T> : IEnumerable<T>
    {
        public WeightErrorHandlingType BadWeightErrorHandling { get; set; } = SetWeightToOne;

        private readonly List<T> _list = new List<T>();
        private readonly List<int> _weights = new List<int>();
        private readonly List<int> _probabilities = new List<int>(); // I like the idea from https://github.com/joseftw/ - scale these up so they're ints. (ie, multiply times Count)
        private readonly List<int> _alias = new List<int>();
        private readonly Random _rand;
        private int _totalWeight;
        private bool _areAllProbabilitiesIdentical = false;

        /// <summary>
        /// Create a new WeightedList with an optional System.Random.
        /// </summary>
        /// <param name="rand"></param>
        public WeightedList(Random rand = null)
        {
            _rand = rand ?? new Random();
        }

        /// <summary>
        /// Create a WeightedList with the provided items and an optional System.Random.
        /// </summary>
        public WeightedList(List<WeightedListItem<T>> listItems, Random rand = null)
        {
            _rand = rand ?? new Random();
            foreach (WeightedListItem<T> item in listItems)
            {
                _list.Add(item._item);
                _weights.Add(item._weight);
            }
            Recalculate();
        }

        public T Next() 
        {
            if (Count == 0) return default;
            int nextInt = _rand.Next(Count); // 0 - n
            if (_areAllProbabilitiesIdentical) return _list[nextInt]; 
            int nextProbability = _rand.Next(_totalWeight);
            return (nextProbability < _probabilities[nextInt]) ? _list[nextInt] : _list[_alias[nextInt]]; 
        }

        /// <summary>
        /// https://www.keithschwarz.com/darts-dice-coins/
        /// </summary>
        private void Recalculate()
        {
            _alias.Clear(); // STEP 1
            _probabilities.Clear(); // STEP 1

            _totalWeight = 0;
            _areAllProbabilitiesIdentical = false;
            List<int> scaledProbabilityNumerator = new List<int>(Count);
            List<int> small = new List<int>(Count); // STEP 2
            List<int> large = new List<int>(Count); // STEP 2
            int minWeight = 0, maxWeight = 0;
            bool isFirst = true;
            foreach (int weight in _weights)
            {
                if (isFirst)
                {
                    minWeight = maxWeight = weight;
                    isFirst = false;
                }
                minWeight = (weight < minWeight) ? weight : minWeight;
                maxWeight = (maxWeight < weight) ? weight : maxWeight;
                _totalWeight += weight;
                scaledProbabilityNumerator.Add(weight * Count); // STEP 3 -- eg for 1/20, 2/20, 3/20, 4/20, 9/20 = {5, 10, 15, 20, 45} - totalweight = 19 (4/5 * 5 = 20/19 3/9 * 5 = 15/19
                _alias.Add(0);
                _probabilities.Add(0);
            }

            // Degenerate case, all probabilities are equal.
            if (minWeight == maxWeight)
            {
                _areAllProbabilitiesIdentical = true;
                return; 
            }

            // STEP 4
            for (int i = 0; i < Count; i++)
            {
                if (scaledProbabilityNumerator[i] < _totalWeight) 
                    small.Add(i);
                else 
                    large.Add(i);
            }

            // STEP 5
            while (small.Count > 0 && large.Count > 0)
            {
                int l = small[0]; // 5.1
                small.RemoveAt(0);
                int g = large[0]; // 5.2
                large.RemoveAt(0);
                _probabilities[l] = scaledProbabilityNumerator[l]; // 5.3
                _alias[l] = g; // 5.4
                int tmp = scaledProbabilityNumerator[g] + scaledProbabilityNumerator[l] - _totalWeight; // 5.5, even though using ints for this algorithm is stable
                scaledProbabilityNumerator[g] = tmp;
                if (tmp < _totalWeight)
                    small.Add(g); // 5.6 the large is now in the small pile
                else
                    large.Add(g); // 5.7 add the large back to the large pile
            }

            // STEP 6
            while (large.Count > 0)
            {
                int g = large[0]; // 6.1
                large.RemoveAt(0);
                _probabilities[g] = _totalWeight; //6.1
            }

            // STEP 7 - I don't think this can happen with ints as the weight.
            while (small.Count > 0)
            {
                int l = small[0]; // 7.1
                small.RemoveAt(0);
                _probabilities[l] = _totalWeight;
            }
        }

        public int TotalWeight => _totalWeight;

        public IReadOnlyList<T> Items => _list.AsReadOnly();

        public IEnumerator<T> GetEnumerator() => _list.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => _list.GetEnumerator();

        public void Add(T item, int weight)
        {
            _list.Add(item);
            _weights.Add(FixWeight(weight));
            Recalculate();
        }

        public void Clear()
        {
            _list.Clear();
            _weights.Clear();
            Recalculate();
        }

        public void Contains(T item) => _list.Contains(item);

        public int IndexOf(T item) => _list.IndexOf(item);

        public void Insert(int index, T item, int weight)
        {
            _list.Insert(index, item);
            _weights.Insert(index, FixWeight(weight));
            Recalculate();
        }

        public void Remove(T item)
        {
            int index = IndexOf(item);
            RemoveAt(index);
            Recalculate();
        }

        public void RemoveAt(int index)
        {
            _list.RemoveAt(index);
            _weights.RemoveAt(index);
            Recalculate();
        }

        public T this[int index] => _list[index];

        public int Count => _list.Count; // O(1), no need to cache.

        public void SetWeight(T item, int newWeight)
        {
            int index = IndexOf(item);
            SetWeightAtIndex(index, FixWeight(newWeight));
        }

        public int GetWeightOf(T item)
        {
            int index = IndexOf(item);
            return GetWeightAtIndex(index);
        }

        public void SetWeightAtIndex(int index, int newWeight)
        {
            _weights[index] = FixWeight(newWeight);
            Recalculate();
        }

        public int GetWeightAtIndex(int index) => _weights[index];

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("WeightedList<");
            sb.Append(typeof(T).Name);
            sb.Append(">: TotalWeight:");
            sb.Append(TotalWeight);
            sb.Append(", Count:");
            sb.Append(Count);
            sb.Append(", {");
            for (int i = 0; i < _list.Count; i++)
            {
                sb.Append(_list[i].ToString());
                sb.Append(":");
                sb.Append(_weights[i].ToString());
                if (i < _list.Count - 1) sb.Append(", ");
            }
            sb.Append("}");
            return sb.ToString();
        }

        // Adjust bad weights silently.
        internal static int FixWeightSetToOne(int weight) => (weight <= 0) ? 1 : weight;

        // Throw an exception when adding a bad weight.
        internal static int FixWeightExceptionOnAdd(int weight) => (weight <= 0) ? throw new ArgumentException("Weight cannot be non-positive") : weight;

        private int FixWeight(int weight)
        {
            if (BadWeightErrorHandling == ThrowExceptionOnAdd) return FixWeightExceptionOnAdd(weight);
            if (BadWeightErrorHandling == ThrowExceptionOnNext) return weight;
            return FixWeightSetToOne(weight);
        }
    }

    public class WeightedListItem<T>
    {
        internal readonly T _item;
        internal readonly int _weight;

        public WeightedListItem(T item, int weight)
        {
            _item = item;
            _weight = weight;
        }
    }

    public enum WeightErrorHandlingType
    {
        SetWeightToOne, // Default
        ThrowExceptionOnAdd,
        ThrowExceptionOnNext,
    }
}
