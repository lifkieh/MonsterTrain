# Google Play Data Safety — Draft answers

Fill this into Play Console → App content → Data safety. Two scenarios depending on
whether Unity Analytics + Google Play Billing are shipped in the final build.

## Recommended (cleanest): remove unused services
The codebase does NOT call Unity Analytics or in-app purchases. Removing
`com.unity.purchasing` and `com.unity.analytics` from `Packages/manifest.json` drops the
`BILLING` permission and all data collection, so Data Safety becomes:
- **Does your app collect or share any user data?** → **No.**
- Data encrypted in transit: N/A. Users can request deletion: N/A (nothing collected).
This is the honest, simplest answer and is recommended for v1.0.0.

## If Unity Analytics / Billing remain
- **Collects data:** Yes.
  - **App activity** (in-app actions, anonymized) — collected, not shared — for Analytics/
    app functionality. Not user-identifiable.
  - **Device or other IDs / diagnostics** (crash logs, device model/OS) — collected, not
    shared — for stability.
  - **Purchase history** (if IAP offered) — processed by Google Play for the purchase.
- Data is encrypted in transit (Unity/Google endpoints use TLS).
- Data collection is not required to use the app's core loop (offline play).
- Provide the privacy-policy URL (docs/PRIVACY_POLICY.md hosted).

**Action item:** decide before submission. v1.0.0 ships offline-only, so removing the two
packages and declaring "No data collected" is the recommended path.
