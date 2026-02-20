# Native Code

Unity supports **Native Plugins**, which are libraries of native code
(e.g. C or Objective-C) that can be called from your C# code.
This is very useful for integrating with the native SDKs on each platform, such
as Apple's "Authorization Services" framework.

The native code you write must expose a "C-compatible" API, but is otherwise
normal native code, that can call all the libraries you want.
The resources below (Mono's docs especially) speak to the details of how data
is converted back and forth between native C and C#.

Some important callouts to list here though:

- Callbacks passed into native code (known there as "function pointers")
  [**need to be static**](https://docs.unity3d.com/Manual/ScriptingRestrictions.html),
  so we use `MonoPInvokeCallback` for a static function and a map
  (`_completionHandlers`) of `IntPtr` to whatever the "actual" callback is as a
  way to map the callback back to a particular instance of the class instead.
- All functions exposed by the native code are brought into your C# class as
  static methods, using `[DllImport(...)]` and the `extern` keyword.

Finally, [Managed Code](https://learn.microsoft.com/en-us/dotnet/standard/managed-code)
is a term that shows up a lot (in contrast to "Unmanaged" code) in this topic.
The gist of it is:

- Managed Code is C# level code, _managed_ by the **.NET** runtime, including
  Garbage Collection.
- Unmanaged Code, on the other hand, is _not_ handled by **.NET**, meaning the
  native code is in charge of managing its memory[^1].

[^1]:
    Native platforms such as iOS with Objective-C have their own memory management processes
    in [ARC](https://en.wikipedia.org/wiki/Automatic_Reference_Counting). What "Unmanaged" means is that there is no
    management
    by .NET, but the native side can still control memory with mechanisms like ARC.

## Native iOS with Objective-C

Objective-C does not have _garbage collection_ per se, but it does operate
under [Automatic Reference Counting](https://clang.llvm.org/docs/AutomaticReferenceCounting.html),
which means when an object is no longer referenced by anyone else (i.e. there
are no more pointers to it), then it will be deallocated from memory.

The important thing to note, is that references to a native class **from C#
code**, via a `void*` type, do **not** count towards ARC, which means if that
is all you have, the native object under your C# wrapper may be removed from
memory before you intend.

The important thing is that when you move between your Objective-C class types
and `void*` pointer types, you must do so consciously, with one of the
following:

- `(__bridge T*)ptr` takes a C# `ptr` and gives you an Objective-C reference,
  without changing anything about ARC (so if it wasn't counted before, it will
  not be counted now).
- `(__bridge void*)object` takes an Objective-C reference and gives you a C#
  pointer, also without affecting ARC. You can think of both as just "casting"
  between types, but know that casting an Objective-C `T*` to a C# `void*` with
  `__bridge` means once the Objective-C `T*` is dropped, the reference count
  **will** decrease, as the C# `void*` does not count. If you are familiar with
  the terminology, you can broadly think of `__bridge` as casting into a **weak**
  pointer on either side.
- `(__bridge_retained void*)object` takes an Objective-C reference `object` and
  gives you a C# pointer `void*`, but in this case, says to ARC that this pointer
  **does count**, and so until it is explicitly released using `CFRelease`, the
  object will not be deallocated.
- `(__bridge_transfer T*)ptr` takes a C# `ptr` and gives you an Objective-C
  reference, again transferring ownership of the memory from C# to Objective-C.

The resources below will do a great job diving deep on each of these!
But the gist of it is:

- Use `(__birdge_retained void*)object` when you have initialized a new
  Objective-C class that you want to manage via a C# wrapper that is
  `IDispoable`.
- When using the object throughout its life, use the basic `__bridge` to go
  back and forth between C# and Objective-C.
- Once the C# `Dispose` method is called, make sure to call `CFRelease` in the
  native layer to clear up the memory.

## Resources

- [Mono Docs: Interop with Native Libraries](https://www.mono-project.com/docs/advanced/pinvoke/).
- [Unity Docs: Native plug-ins](https://docs.unity3d.com/Manual/NativePlugins.html).
- [Apple Docs: Toll-Free Bridged Types](https://developer.apple.com/library/archive/documentation/CoreFoundation/Conceptual/CFDesignConcepts/Articles/tollFreeBridgedTypes.html).
- [Clang Docs: Objective-C Automatic Reference Counting](https://clang.llvm.org/docs/AutomaticReferenceCounting.html).
