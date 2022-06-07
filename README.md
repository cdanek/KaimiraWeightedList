# KaimiraGames Weighted List

## Introduction

This C# class adds an integer weight to each element of a list, allowing you to draw one randomly using `Next()`. The solution is implemented using the Walker-Vose "Alias Method", which is extremely fast (O(1) for gets), space efficient (O(n) memory use) with reasonable recalculation costs (O(n) for any operations). It can be used with any type, similarly to a `List<T>`.

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

Since adding items incurs a recalculation of weights, if you'd like to add many items at once, you can do so with a list of `WeightedListItem<T>` items. This will only recalculate the weights once. For large lists, this is recommended.

```cs
List<WeightedListItem<string>> myItems = new()
{
    new WeightedListItem<string>("Hello", 10),
    new WeightedListItem<string>("World", 20),
};
WeightedList<string> myWL = new(myItems);
```

### Bad Weights

Programming is opinionated, and I'm of the opinion that an error in weighting shouldn't throw an exception - it's better to get bad data than have your application crash. If you'd like your application to throw exceptions on non-positive weights (either on `Add()` or `Next()`), then configure like so:

```cs
WeightedList<string> myWL = new();
myWL.BadWeightErrorHandling = WeightErrorHandlingType.ThrowExceptionOnAdd;
myWL.Add("Goodbye, World.", 0); // ArgumentException - Weight cannot be non-positive.

myWL.BadWeightErrorHandling = WeightErrorHandlingType.SetWeightToOne; // default
myWL.Add("Hello, World.", 0); // No error, but will set the weight to 1.

myWL.BadWeightErrorHandling = WeightErrorHandlingType.ThrowExceptionOnNext; 
myWL.Add("Hello, World.", 0); // No error.
myWL.Next(); // erratic behaviour
```

### Seeded Random

If you'd like to see the list with your own System.Random, do so with:

```cs
System.Random rand = new System.Random();
WeightedList<string> myWL = new(rand);
```

### API

```cs
public class WeightedList<T> : IEnumerable<T>
{
    public WeightedList(Random rand = null)
    public WeightedList(List<WeightedListItem<T>> listItems, Random rand = null)

    public T this[int index]
    public int Count
    public int TotalWeight
    public IReadOnlyList<T> Items
    public WeightErrorHandlingType BadWeightErrorHandling = SetWeightToOne;

    public void Add(T item, int weight)
    public T Next() 

    public void Clear()
    public void Contains(T item)
    public int IndexOf(T item)
    public void Insert(int index, T item, int weight)
    public IEnumerator<T> GetEnumerator()
    public int GetWeightAtIndex(int index)
    public int GetWeightOf(T item)
    public void Remove(T item)
    public void RemoveAt(int index)
    public void SetWeight(T item, int newWeight)
    public void SetWeightAtIndex(int index, int newWeight)
    public string ToString()
}

public class WeightedListItem<T>
{
    public WeightedListItem(T item, int weight)
}

public enum WeightErrorHandlingType
{
    SetWeightToOne,
    ThrowExceptionOnAdd,
    ThrowExceptionOnNext,
}
```

## License

Distributed under the MIT License. See `LICENSE.txt` for more information.

## Acknowledgments

* [Walker-Vose Alias algorithm](https://en.wikipedia.org/wiki/Alias_method)
* [joseftw's implementation](https://github.com/joseftw/jos.weightedresult)
* [Keith Schwarz's excellent explanation of why this works](https://www.keithschwarz.com/darts-dice-coins/)
