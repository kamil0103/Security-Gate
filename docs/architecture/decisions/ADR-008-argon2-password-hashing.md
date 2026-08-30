# ADR-008: Use Argon2id for Password Hashing

## Status

Accepted

## Context

The Security Gateway stores user passwords. Passwords must never be stored in plaintext and must use a modern, memory-hard hashing algorithm to resist brute-force and hardware-accelerated attacks.

## Decision

Use Argon2id for password hashing via the `Konscious.Security.Cryptography.Argon2` library.

Parameters:

- Salt length: 16 bytes
- Hash length: 32 bytes
- Degree of parallelism: 4
- Memory: 64 MB
- Iterations: 3

## Alternatives

- **PBKDF2:** Widely supported but not memory-hard; more vulnerable to GPU/ASIC attacks.
- **bcrypt:** Memory-hard but limited to 72-byte passwords and lacks parallelism tuning.
- **scrypt:** Memory-hard but older and less flexible than Argon2.

## Consequences

- Password hashes resist GPU and ASIC attacks due to memory hardness.
- Hash verification is slower than PBKDF2, which is acceptable for an authentication endpoint.
- The hash format is self-describing and includes salt and parameters.
