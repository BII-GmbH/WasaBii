# Utils/FunctionalUtils Module

## Overview

This module contains generic functional utilities and data structures which cannot be extension methods.

## Utils.IfAllNotNull 

Executes code only when two values are both not null.

## SingeLinkedList<T>

This is a simple implementation of an immutable single-linked list as a reusable `IEnumerable<T>`.
The design is heavily inspired by Haskell: You can only add values by using `.Prepend(T)`, and 
doing so returns a new linked list object. This reuses the original list instead of copying the contents, 
while not modifying the original reference.

This data structure is especially valuable when traversing a non-linear recursive data structure 
(like a directory tree) while remembering information while traversing (e.g. the path traversed so far),
as different recursion branches can reuse the same data. 
