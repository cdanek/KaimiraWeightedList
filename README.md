# Kaimira Weighted List (C#)

## Introduction

TLDR: Add items to a list with an integer weight, and get items randomly from the list based on the weight. 

This class implements an algorithm for sampling from a discrete probability distribution via a generic list with extremely fast `O(1)` get operations and small (close to minimally small) `O(n)` space complexity and `O(n)` CRUD complexity. 

In other words, you can add any item of type `T` to a `List` with an integer weight, and get a random item from the list with probability ( weight / sum-weights ). The solution is implemented using the [Walker-Vose "Alias Method"](https://en.wikipedia.org/wiki/Alias_method). 

It can be used with any type, similarly to a `List<T>`, and was designed to be fast AND as easy to use as possible.

## Installation and Usage

To use this project, simply download the `WeightedList.cs` file and drop it in a convenient place in your solution. Declare and initialize the `WeightedList`, and add items with `Add(T item, int weight)`. Get an item with `Next()`. Easy. 

```cs
using KaimiraGames;

WeightedList<string> myWL = new();
myWL.Add("Hello", 10);
myWL.Add("World", 20);

Console.WriteLine(myWL.Next()); // Hello 33% of the time, World 66% of the time.
``` 

## Advanced

### Reducing Add() Costs

Since adding items incurs a recalculation of weights, if you'd like to add many items at once, you can do so with an `ICollection` of `WeightedListItem<T>` items. This will only recalculate the weights once. For large lists, this is recommended.

```cs
List<WeightedListItem<string>> myItems = new()
{
    new WeightedListItem<string>("Hello", 10),
    new WeightedListItem<string>("World", 20),
};
WeightedList<string> myWL = new(myItems);
```

### Bad Weights

Programming is opinionated, and I'm of the opinion that an error in weighting shouldn't throw an exception - it's better to get bad data than have your application crash. Using the default settings will prevent the weight of any item from ever going below 1 using `AddWeightToAll()`, `SubtractWeightFromAll()`, `SetWeightOfAll()`, `SetWeight()`, `SetWeightAtIndex()`, `Add()`, or `Insert()`. **Note that this is the default behaviour (silent adjustment of bad weights to 1).**

If, however, you'd like your application to throw exceptions on non-positive weights (on `Add()`), then configure like so:

```cs
WeightedList<string> myWL = new();
myWL.BadWeightErrorHandling = WeightErrorHandlingType.ThrowExceptionOnAdd;

myWL.Add("Goodbye, World.", 0); // ArgumentException - Weight cannot be non-positive.
myWL.Insert("Goodbye, World.", 0); // ArgumentException - Weight cannot be non-positive.
myWL.Add("Goodbye, World.", 5); // OK
myWL.SetWeight("Goodbye, World.", 0); // ArgumentException - Weight cannot be non-positive.
myWL.SetWeightOfAll(0); // ArgumentException - Weight cannot be non-positive.
myWL.SetWeightAtIndex(0, 0); // ArgumentException - Weight cannot be non-positive.
myWL.AddWeightFromAll(-5); // ArgumentException - Subtracting 5 from all items would set weight to non-positive for at least one element.
myWL.SubtractWeightFromAll(5); // ArgumentException - Subtracting 5 from all items would set weight to non-positive for at least one element.
myWL.SubtractWeightFromAll(4); // OK - weight is now 1 

myWL.BadWeightErrorHandling = WeightErrorHandlingType.SetWeightToOne; // default
myWL.Add("Hello, World.", 0); // No error, but will set the weight to 1.
myWL.Add("Hello, World.", -1); // Also will set the weight to 1.
// ... etc ... All error actions set the weight of individual elements that would be non-positive to 1.
```

### Seeded Random

If you'd like to seed the list with your own System.Random, do so with:

```cs
System.Random rand = new System.Random();
WeightedList<string> myWL = new(rand);
```

### API

I've added a number of methods that appear in `IList<T>`. 

Be aware that while you are _able_ to modify the weight while iterating (by getting an iterator, getting the index of the element, and setting the weight of the element at said index) is not a great idea. It works, but will recalculate on every iteration (which means O(n<sup>2</sup>) performance for your loop). 

If you want to modify the weight of everything in the list, use `AddWeightToAll(int)`, `SubtractWeightFromAll(int)`, or `SetWeightOfAll(int)`.

```cs
public class WeightedList<T> : IEnumerable<T>
{
    public WeightedList(Random rand = null);
    public WeightedList(ICollection<WeightedListItem<T>> listItems, Random rand = null);

    public T this[int index];
    public int Count;
    public int TotalWeight;
    public int MinWeight;
    public int MaxWeight;
    public IReadOnlyList<T> Items;
    public WeightErrorHandlingType BadWeightErrorHandling = SetWeightToOne;

    public void Add(T item, int weight);
    public void Add(ICollection<WeightedListItem<T>> listItems);
    public T Next();

    public void AddWeightToAll(int weight);
    public void Clear();
    public void Contains(T item);
    public IEnumerator<T> GetEnumerator();
    public int GetWeightAtIndex(int index);
    public int GetWeightOf(T item);
    public int IndexOf(T item);
    public void Insert(int index, T item, int weight);
    public void Remove(T item);
    public void RemoveAt(int index);
    public void SetWeight(T item, int newWeight);
    public void SetWeightOfAll(int weight);
    public void SetWeightAtIndex(int index, int newWeight);
    public void SubtractWeightFromAll(int weight);
    public string ToString();
}

public class WeightedListItem<T>
{
    public WeightedListItem(T item, int weight);
}

public enum WeightErrorHandlingType
{
    SetWeightToOne, // Default
    ThrowExceptionOnAdd, // Throw exception for adding non-positive weight.
}
```

## Notes

This algorithm is strictly better (better across all dimensions) than any others known to the author for each of:

1) Generate operations (`Next()`) - O(1)
2) "CRUD" operations (`private Recalculate()`, called on any operations that change any weight) - O(n)
3) Memory usage / Space complexity (in the resting state, elements are limited to one `List<T>`, one `enum`, three `int`s, one `Random`, one `bool`, and three `List<int>`s; in the calculating states we add a few working variables) - O(n)

I have made one small improvement based on the idea from [joseftw](https://github.com/joseftw/), which eliminates all of the instability of floating point numbers by enforcing integer weight. This leads to "perfect" filling of the alias/probability matrix (described by Keith Schwarz) with the downside of limiting total weight to `1 / Int32.IntMax` in C#: approximately 2.1 million. If you need accurate probabilities with greater precision for very small chances, you can change all instances of `int` to `long`, giving you total weight of up to 1 in 9.2 quintillion (9.2e18) at the cost of increased memory use. You'd also need to upgrade the Random number generator, as `Random.NextInt64()` doesn't appear in .NET until .NET6 (and I didn't want to place that requirement here).

This should be large enough for most conventional use of the structure - it will be accurate for around up to 1000 items with average weights of 2000. 

If you need accurate probabilities with enough items such that the total weight exceeds this, I'd suggest either evaluating your weights and "reducing" them by an acceptable amount periodicially OR upgrading the code to use `long` and `Rand.NextInt64()`. 

## License

Distributed under the MIT License. See `LICENSE` for more information.

## Acknowledgments

* [Michael Vose's Alias algorithm](https://en.wikipedia.org/wiki/Alias_method)
* [joseftw's implementation, C#](https://github.com/joseftw/jos.weightedresult)
* [Keith Schwarz's excellent explanation of why this works](https://www.keithschwarz.com/darts-dice-coins/)
