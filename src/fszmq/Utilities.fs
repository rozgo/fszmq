(* ------------------------------------------------------------------------
This file is part of fszmq.

This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
------------------------------------------------------------------------ *)
namespace fszmq

open System
open System.Collections.Generic
open System.Runtime.CompilerServices
open System.Text

/// Contains methods for working with ZMQ's proxying capabilities
[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module Proxying =

  /// creates a proxy connection passing messages between two sockets,
  /// with an (optional) third socket for supplemental data capture
  [<CompiledName("Proxy")>]
  let proxy (frontend:Socket) (backend:Socket) (capture:Socket option) =
    match capture with
    | Some capture -> C.zmq_proxy(frontend.Handle,backend.Handle,capture.Handle)
    | _            -> C.zmq_proxy(frontend.Handle,backend.Handle,            0n)
    |> ignore

/// Utilities for working with Polling from languages other than F#
[<Extension>]
type ProxyingExtensions =

  /// creates a proxy connection passing messages between two sockets
  [<Extension>]
  static member Proxy(frontend,backend) = Proxying.proxy frontend backend None

  /// creates a proxy connection passing messages between two sockets,
  /// with an third socket for supplemental data capture (e.g. logging)
  [<Extension>]
  static member Proxy(frontend,backend,capture) = Proxying.proxy frontend backend (Some capture)

/// Utilities for working with ZeroMQ Base-85 Encoding
[<RequireQualifiedAccess>]
module Z85 =

  /// Encodes a binary block into a string using ZeroMQ Base-85 Encoding.
  ///
  /// ** Note: the size of the binary block MUST be divisible be 4. **
  [<CompiledName("Encode")>]
  let encode data =
    let datalen = Array.length data
    if  datalen = 0 then ZMQ.einval "Invalid argument"
    let buffer  = StringBuilder (datalen * 5 / 4 + 1)
    if C.zmq_z85_encode(buffer,data,unativeint datalen) = 0n then ZMQ.error()
    string buffer

  /// Decodes ZeroMQ Base-85 encoded string to a binary block.
  ///
  /// ** Note: the size of the string MUST be divisible be 5. **
  [<CompiledName("Decode")>]
  let decode data =
    let datalen = String.length data
    if  datalen = 0 then ZMQ.einval "Invalid argument"
    let buffer  = Array.zeroCreate (datalen * 4 / 5)
    if C.zmq_z85_decode(buffer,data) = 0n then ZMQ.error()
    buffer

/// Utilities for working with the CurveZMQ security protocol
[<Experimental("WARNING: Functionality in the Curve module requires more testing.")>]
module Curve =

  let [<Literal>] private KEY_SIZE = 41 //TODO: should this be hard-coded?

  /// Returns a newly generated random keypair consisting of a public key and a secret key.
  /// The keys are encoded using ZeroMQ Base-85 Encoding.
  [<CompiledName("MakeCurveKeyPair")>]
  let curveKeyPair () =
    let publicKey,secretKey = StringBuilder(KEY_SIZE),StringBuilder(KEY_SIZE)
    if C.zmq_curve_keypair(publicKey,secretKey) <> 0 then ZMQ.error()
    (string publicKey),(string secretKey)

//NOTE: This allows non-F# extensions to have proper visibility/interop with all CLR languages
[<assembly: ExtensionAttribute()>]
do()
