# CryptoUtility

A static utility class providing cryptographic helper methods for generating secure hashes, HMAC signatures, and random data. Designed for use in API gateway scenarios where message integrity, authentication, and random token generation are required.

## API

### GenerateSha256Hash

Generates a SHA-256 hash of the input string using UTF-8 encoding.

- **Parameters**
  - `input` (string): The string to hash.
- **Return value**
  - (string): The hexadecimal representation of the SHA-256 hash.
- **Exceptions**
  - Throws `ArgumentNullException` if `input` is `null`.

### GenerateHmacSha256

Generates an HMAC-SHA256 signature of the input string using the provided secret key.

- **Parameters**
  - `input` (string): The string to sign.
  - `secret` (string): The secret key used for signing.
- **Return value**
  - (string): The hexadecimal representation of the HMAC-SHA256 signature.
- **Exceptions**
  - Throws `ArgumentNullException` if either `input` or `secret` is `null`.

### VerifyHmacSha256

Verifies an HMAC-SHA256 signature against the input string and secret key.

- **Parameters**
  - `input` (string): The string to verify.
  - `secret` (string): The secret key used for verification.
  - `signature` (string): The expected HMAC-SHA256 signature to compare against.
- **Return value**
  - (bool): `true` if the computed signature matches `signature`; otherwise, `false`.
- **Exceptions**
  - Throws `ArgumentNullException` if any of `input`, `secret`, or `signature` is `null`.

### GenerateRandomString

Generates a cryptographically secure random alphanumeric string of the specified length.

- **Parameters**
  - `length` (int): The desired length of the random string.
- **Return value**
  - (string): A random string composed of alphanumeric characters.
- **Exceptions**
  - Throws `ArgumentOutOfRangeException` if `length` is less than zero.

### GenerateRandomBytes

Generates a cryptographically secure array of random bytes of the specified length.

- **Parameters**
  - `length` (int): The desired length of the byte array.
- **Return value**
  - (byte[]): An array of random bytes.
- **Exceptions**
  - Throws `ArgumentOutOfRangeException` if `length` is less than zero.

## Usage
