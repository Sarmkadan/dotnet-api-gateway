# Webhook Signature Verification

## Overview

All webhook deliveries are signed using HMAC-SHA256 to ensure authenticity and integrity. Each outgoing webhook request includes an `X-Signature` header that recipients can use to verify the payload originated from this gateway and has not been tampered with.

## Signature Format

The `X-Signature` header uses the following format:

```
X-Signature: t=<timestamp>,v1=<signature>
```

- `t`: Unix timestamp in seconds (for replay protection)
- `v1`: HMAC-SHA256 signature using the current secret

## Signature Generation

The signature is computed as:

```
signature = HMAC-SHA256(secret, "<timestamp>.<json_payload>")
```

Where:
- `secret` is the current secret for the subscription
- `timestamp` is the current Unix timestamp in seconds
- `json_payload` is the JSON-serialized webhook event body

## Verification Process

To verify a webhook signature:

1. Extract the timestamp and signature from the `X-Signature` header
2. Verify the timestamp is recent (within the last 5 minutes to prevent replay attacks)
3. Compute the expected signature using the secret you configured for the subscription
4. Compare the computed signature with the received signature using constant-time comparison

### Example (Python)

```python
import hmac
import hashlib
import time

def verify_signature(payload_body, signature_header, secret):
    # Extract timestamp and signature
    parts = signature_header.split(',')
    timestamp = parts[0].split('=')[1]
    received_signature = parts[1].split('=')[1]
    
    # Verify timestamp is recent (within 5 minutes)
    current_time = int(time.time())
    if abs(current_time - int(timestamp)) > 300:
        return False
    
    # Compute expected signature
    message = f"{timestamp}.{payload_body}"
    expected_signature = hmac.new(
        secret.encode('utf-8'),
        message.encode('utf-8'),
        hashlib.sha256
    ).hexdigest()
    
    # Constant-time comparison
    return hmac.compare_digest(expected_signature, received_signature)
```

### Example (Node.js)

```javascript
const crypto = require('crypto');

function verifySignature(payloadBody, signatureHeader, secret) {
    // Extract timestamp and signature
    const parts = signatureHeader.split(',');
    const timestamp = parts[0].split('=')[1];
    const receivedSignature = parts[1].split('=')[1];
    
    // Verify timestamp is recent (within 5 minutes)
    const currentTime = Math.floor(Date.now() / 1000);
    if (Math.abs(currentTime - parseInt(timestamp)) > 300) {
        return false;
    }
    
    // Compute expected signature
    const message = `${timestamp}.${payloadBody}`;
    const expectedSignature = crypto
        .createHmac('sha256', secret)
        .update(message)
        .digest('hex');
    
    // Constant-time comparison
    return crypto.timingSafeEqual(
        Buffer.from(expectedSignature),
        Buffer.from(receivedSignature)
    );
}
```

## Secret Management

### Creating a Subscription

When creating a subscription, a secure random secret is automatically generated and returned:

```http
POST /api/WebhookManagement/subscriptions
Content-Type: application/json

{
  "callbackUrl": "https://example.com/webhook",
  "eventTypes": ["order.created", "order.updated"]
}
```

Response:
```json
{
  "id": "...",
  "callbackUrl": "https://example.com/webhook",
  "eventTypes": ["order.created", "order.updated"],
  "currentSecret": "a1b2c3d4e5f6...",
  "active": true,
  "createdAt": "2024-07-24T10:00:00Z",
  "retryPolicy": { ... }
}
```

**Important**: The `currentSecret` is only shown once in the response. Store it securely!


### Providing Your Own Secret

You can provide your own secret during subscription creation:

```http
POST /api/WebhookManagement/subscriptions
Content-Type: application/json

{
  "callbackUrl": "https://example.com/webhook",
  "eventTypes": ["order.created"],
  "secret": "my-very-secure-secret-12345"
}
```

The secret must be at least 16 characters long.

### Rotating Secrets

To rotate a secret:

```http
POST /api/WebhookManagement/subscriptions/{id}/rotate-secret
```

This generates a new secret and keeps the old one active for backward compatibility. The response contains only the new secret:

```json
{
  "secret": "new-secret-value"
}
```

**Important**: Update your webhook endpoint to use the new secret before the old one expires (default rotation keeps both secrets active indefinitely).


### Retrieving the Current Secret

To retrieve the current secret for a subscription:

```http
GET /api/WebhookManagement/subscriptions/{id}/secret
```

Response:
```json
{
  "secret": "current-secret-value"
}
```

## Security Considerations

1. **Always verify signatures**: Never trust webhook payloads without verifying the signature
2. **Use HTTPS**: Ensure your webhook endpoint uses HTTPS to prevent interception
3. **Store secrets securely**: Treat webhook secrets like API keys - store them encrypted
4. **Rotate periodically**: Regularly rotate secrets to maintain security
5. **Validate timestamps**: Check that timestamps are recent to prevent replay attacks
6. **Use constant-time comparison**: When verifying signatures to prevent timing attacks

## Error Handling

If signature verification fails, the webhook delivery should be rejected with an appropriate HTTP 401 Unauthorized status.

## Testing

You can test signature verification using the following test payload and secret:

```json
{
  "eventType": "test",
  "timestamp": "2024-07-24T10:00:00Z",
  "data": { "message": "Test event" },
  "retryCount": 0,
  "signedAt": 1721781600
}
```

With secret `test-secret-1234567890` and timestamp `1721781600`, the signature would be:

```
t=1721781600,v1=3f7e3b2e8f5a9c1d4b6e7f8a9b0c1d2e3f4a5b6c7d8e9f0a1b2c3d4e5f6a7b8
```
