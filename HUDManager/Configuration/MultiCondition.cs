using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace HUDManager.Configuration;

/// <summary>
/// A condition that determines its value based on one or more other conditions.
/// </summary>
public class MultiCondition
{
    [JsonProperty]
    private readonly List<MultiConditionItem> Items = [];
    public int Count => Items.Count;

    public bool IsActive(Plugin plugin)
    {
        if (Items.Count == 0)
            return false;

        bool status = true;

        List<MultiConditionItem> toRemove = new();

        bool first = true; // it's quicker than Select(i, v) and then checking i == 0
        foreach (var item in Items) {
            if (item.Condition.CurrentType == typeof(CustomCondition) && !plugin.Config.CustomConditions.Contains(item.Condition.Custom!)) {
                toRemove.Add(item);
                continue;
            }

            if (first) {
                status = item.Condition.IsActive(plugin) ^ item.Negation;
                first = false;
                continue;
            }

            if (item.Type is MultiConditionJunction.LogicalAnd)
                status &= item.Condition.IsActive(plugin) ^ item.Negation;
            else if (item.Type is MultiConditionJunction.LogicalOr)
                status |= item.Condition.IsActive(plugin) ^ item.Negation;
        }

        foreach (var item in toRemove)
            Items.Remove(item);

        return status;
    }

    public MultiConditionItem this[int i] => Items[i];

    /// <returns>True if the condition can be added without creating a loop, otherwise false.</returns>
    public bool AddCondition(MultiConditionItem item, int index = -1)
    {
        if (index >= 0)
            Items.Insert(index, item);
        else
            Items.Add(item);

        if (!Validate()) {
            Items.Remove(item);
            return false;
        }

        return true;
    }

    public void RemoveCondition(int index)
    {
        Items.RemoveAt(index);
    }

    /// <summary>
    /// Searches for loops within a map of connected multi-conditions.
    /// </summary>
    /// <returns>True if there are no loops, otherwise false.</returns>
    public bool Validate()
    {
        return Validate(null, null);
    }

    private bool Validate(List<MultiCondition>? visitedConditions, MultiCondition? searchCond)
    {
        visitedConditions ??= [];

        if (searchCond is null)
            searchCond = this;
        else
            visitedConditions.Add(this);

        foreach (var cond in AllItems
                     .Where(c => c.Condition.CurrentType == typeof(CustomCondition)
                         && c.Condition.Custom!.ConditionType == CustomConditionType.MultiCondition)) {
            if (visitedConditions.Contains(cond.Condition.Custom!.MultiCondition))
                continue;

            if (cond.Condition.Custom!.MultiCondition == searchCond)
                return false;

            if (!cond.Condition.Custom.MultiCondition.Validate(visitedConditions, searchCond))
                return false;
        }

        return true;
    }

    [JsonIgnore]
    public IReadOnlyCollection<MultiConditionItem> AllItems => Items.AsReadOnly();

    [Serializable]
    public class MultiConditionItem
    {
        public MultiConditionJunction Type;
        public CustomConditionUnion Condition;
        public bool Negation;
    }
}

public enum MultiConditionJunction
{
    [Display(Name = "AND")]
    LogicalAnd,
    [Display(Name = "OR")]
    LogicalOr,
}
