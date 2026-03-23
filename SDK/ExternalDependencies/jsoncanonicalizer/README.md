# JSON Canonicalizer for Unity (UPM)

This is a Unity Package Manager (UPM) fork of the .NET/C# implementation of **JCS (JSON Canonicalization Scheme)** from the [json-canonicalization](https://github.com/cyberphone/json-canonicalization) repository by [Anders Rundgren](https://github.com/cyberphone).

JCS is defined in [RFC 8785](https://www.rfc-editor.org/rfc/rfc8785).

## Original Project

The original source code lives at:
https://github.com/cyberphone/json-canonicalization/tree/master/dotnet

All credit for the canonicalization implementation goes to the original authors. This repository only repackages the .NET code for use as a Unity package.

## What is JCS?

JCS (JSON Canonicalization Scheme) makes JSON data safe for cryptographic operations like hashing and signing by producing deterministic serialization output:

- Primitive JSON types are serialized per ECMAScript `JSON.stringify()` rules
- JSON `Object` properties are sorted lexicographically (recursively)
- JSON `Array` element order is preserved

## Installation

### Via Git URL (Unity 2019.3+)

Add the following to your `Packages/manifest.json`:

```json
{
  "dependencies": {
    "com.privy-io.jsoncanonicalizer": "https://github.com/privy-io/jsoncanonicalizer-upm.git#main"
  }
}
```

Or in the Unity Editor: **Window > Package Manager > + > Add package from git URL** and enter:

```
https://github.com/privy-io/jsoncanonicalizer-upm.git#main
```

## Usage

```csharp
using Org.Webpki.JsonCanonicalizer;

// From a JSON string
var canonicalizer = new JsonCanonicalizer("{\"b\":2,\"a\":1}");
string result = canonicalizer.GetEncodedString();
// result: {"a":1,"b":2}

// From UTF-8 bytes
byte[] jsonBytes = System.Text.Encoding.UTF8.GetBytes("{\"b\":2,\"a\":1}");
var canonicalizer2 = new JsonCanonicalizer(jsonBytes);
byte[] resultBytes = canonicalizer2.GetEncodedUTF8();
```

## License

Apache License 2.0 - see [LICENSE](LICENSE) for details.

Original copyright: Anders Rundgren / WebPKI.org
