namespace TDesu.FSharp.Collections

open System
open System.Collections.Generic
open TDesu.FSharp
open TDesu.FSharp.Operators

module Stack =
    /// Returns the top element as Some, or None if the stack is empty or null.
    /// <param name="stack">The stack to peek into.</param>
    let inline tryPeek (stack: Stack<'T>) =
        if isNull stack then
            None
        else
            let x, y = stack.TryPeek()
            if x then
                Some y
            else
                None

    /// Removes and returns the top element from the stack.
    /// A null <paramref name="stack"/> is treated as an empty stack, so it raises the same
    /// <see cref="System.InvalidOperationException"/> a real empty stack would, not a null-reference error.
    /// <exception cref="System.InvalidOperationException">When the stack is empty, including when <paramref name="stack"/> is null.</exception>
    /// <param name="stack">The stack to pop from.</param>
    let inline pop (stack: Stack<'T>) =
        match stack with
        | null ->
            raise (invalidOp "Stack empty.")
        | _ ->
            stack.Pop()

    /// Pushes an item onto the top of the stack.
    /// <paramref name="stack"/> is the mutation target: unlike the read-only helpers in this module, a
    /// null <paramref name="stack"/> has no empty instance to push onto, so it is a programmer error.
    /// <exception cref="System.ArgumentNullException">When <paramref name="stack"/> is null.</exception>
    /// <param name="item">The item to push.</param>
    /// <param name="stack">The stack to push onto.</param>
    let inline push (item: 'T) (stack: Stack<'T>) =
        Guard.notNull "stack" stack
        stack.Push(item)

    /// Returns a new stack with elements in reverse order. A null <paramref name="stack"/> is treated as
    /// empty and yields a new empty stack.
    /// <param name="stack">The stack to reverse.</param>
    let reverse (stack: Stack<'T>) =
        let newStack = Stack<'T>()
        match stack with
        | null ->
            newStack
        | _ ->
            for item in stack do
                newStack.Push(item)
            newStack
