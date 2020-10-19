# Overview

1. What is CHIP-8?
2. What does CHIP-8 have to do with dotnet?
3. Why is C# a good choice for emulator development?


## Intro

Hello, my name is Eric Carter. I've been a C# developer for more years than I'd like to think about, but even today C# manages to impress me with how well it can handle almost any problem. And that problem today is writing an emulator for the somewhat obscure technology from the 1970s: CHIP-8.

## What is CHIP-8?

To understand what CHIP-8 is and why it was created, we need to understand what computers were like in the 1970s. They were simple devices with impossibly low specs relative to our modern-day equivalents. All software engineers had to work with was a CPU running at 1Mhz and a few kilobytes of RAM. Also software written for one computer was not necessarily compatible with a different model. This made it difficult for hobbyist developers to share programs among their peers. This was the reason why Joseph Weisbecker created the CHIP-8 language in the mid-1970s. CHIP-8 allows programs to be written once and run on many different models of computers.

> Show https://en.wikipedia.org/wiki/CHIP-8

## What does CHIP-8 have to do with dotnet?

CHIP-8 programs appear similar to traditional assembly programs. The fundamental difference between CHIP-8 and assembly is the output of compilation isn't native code. CHIP-8 applications run on interpreted binary instructions, which is somewhat similar to dotnet and MSIL. Because of this, I think of CHIP-8 as a distant ancestor to C# and MSIL. But more importantly, CHIP-8 is one of the easiest things to write an emulator for and that's what I'm going to show you today.

> Quick demo of PONG or something

> Show basic implementation so people roughly understand the code:
> 1. Loading a ROM into memory
> 2. Cycles
> 3. Instructions

## Why is C#/dotnet a good choice for emulator development?

Most emulator developers write their code in a low-level language, like C, C++ or more recently Rust. So why did I use C# to write this emulator? One of C#'s best traits is that it can be whatever language you want it to be. Do you like like functional programming? Use LINQ. Do you hate types? `dynamic` is the keyword for you. Do you want to write some blazing-fast low-level code? C# has you covered!

## C# Low-Level

Since version 1.0 of C#, the language has had support for pointers. That was nearly 20yrs ago. So let's have a quick poll: How any of you have shipped code that used pointers in C#?

Despite C# always having pointer support, using pointers in the language has only _recently_ started to get really good. With the introduction of `System.Span` and `System.Memory` structs, dealing with pinned memory is a lot easier. `System.Buffer` simplifies memory copies. Also the additions of `System.Numerics.Vector` has given C# devs access to power SIMD instructions.

> Low-level features:
> * Memory/Span: MemoryHandle removes the need to pin/unpin
> * Vector: Render a whole line of a sprite in one shot
