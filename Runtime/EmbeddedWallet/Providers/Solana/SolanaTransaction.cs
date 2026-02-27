using System;

namespace Privy
{
    /// <summary>
    /// Parses and assembles a serialized Solana transaction.
    /// Ported from the iOS SDK's SolanaTransaction.swift.
    /// </summary>
    internal class SolanaTransaction
    {
        /// <summary>
        /// The raw message bytes extracted from the serialized transaction.
        /// </summary>
        internal byte[] Message { get; }

        /// <summary>
        /// Parses the encoded bytes of a Solana transaction and extracts the message portion.
        /// </summary>
        /// <param name="bytes">The serialized transaction bytes.</param>
        /// <exception cref="PrivyException.EmbeddedWalletException">
        /// Thrown when the bytes are too short, the signature count cannot be read, or no message bytes are found.
        /// </exception>
        internal SolanaTransaction(byte[] bytes)
        {
            if (bytes == null || bytes.Length < 4)
            {
                throw new PrivyException.EmbeddedWalletException(
                    "Transactions have a minimum length of 4 bytes",
                    EmbeddedWalletError.RpcRequestFailed);
            }

            if (!TryReadCompactU16(bytes, 0, out ushort signatureCount, out int nextPosition))
            {
                throw new PrivyException.EmbeddedWalletException(
                    "Unable to read the signature count",
                    EmbeddedWalletError.RpcRequestFailed);
            }

            // The Compact U16 value occupies (nextPosition - 0) bytes.
            // Signatures start right after the compact-u16 header.
            int signatureHeaderLength = nextPosition - 1;
            int signaturePayloadLength = 64 * (int)signatureCount;
            int signatureBlockLength = signatureHeaderLength + signaturePayloadLength;

            int messagePosition = signatureBlockLength + 1;
            if (messagePosition >= bytes.Length)
            {
                throw new PrivyException.EmbeddedWalletException(
                    "No message bytes found after signatures",
                    EmbeddedWalletError.RpcRequestFailed);
            }

            int messageLength = bytes.Length - messagePosition;
            Message = new byte[messageLength];
            Array.Copy(bytes, messagePosition, Message, 0, messageLength);
        }

        /// <summary>
        /// Builds a signed transaction by prepending a single Ed25519 signature to the message bytes.
        /// Format: [0x01][64-byte signature][message bytes]
        /// </summary>
        /// <param name="signature">The 64-byte Ed25519 signature over the message bytes.</param>
        /// <returns>The fully assembled signed transaction bytes.</returns>
        /// <exception cref="PrivyException.EmbeddedWalletException">
        /// Thrown when the signature is not exactly 64 bytes.
        /// </exception>
        internal byte[] AddSignature(byte[] signature)
        {
            if (signature == null || signature.Length != 64)
            {
                throw new PrivyException.EmbeddedWalletException(
                    "The computed signature for the transaction must be 64 bytes long",
                    EmbeddedWalletError.RpcRequestFailed);
            }

            // Format: [0x01][64-byte signature][message bytes]
            byte[] result = new byte[1 + 64 + Message.Length];
            result[0] = 0x01;
            Array.Copy(signature, 0, result, 1, 64);
            Array.Copy(Message, 0, result, 65, Message.Length);
            return result;
        }

        /// <summary>
        /// Reads a Compact U16 variable-length encoded integer from the byte array.
        /// Compact U16 uses up to 3 bytes; each byte contributes 7 bits, with the
        /// high bit indicating that more bytes follow.
        /// </summary>
        private static bool TryReadCompactU16(byte[] data, int position, out ushort result, out int nextPosition)
        {
            result = 0;
            nextPosition = position;

            for (int offset = 0; offset <= 2; offset++)
            {
                int ix = position + offset;
                int shift = 7 * offset;

                if (ix >= data.Length)
                {
                    return false;
                }

                byte b = data[ix];
                result |= (ushort)((b & 0x7F) << shift);

                if ((b & 0x80) == 0)
                {
                    // High bit not set — we're done
                    nextPosition = ix + 1;
                    return true;
                }
            }

            // All 3 bytes used and the last had the high bit set — invalid encoding
            return false;
        }
    }
}
