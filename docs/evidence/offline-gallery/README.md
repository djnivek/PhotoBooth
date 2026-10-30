# Offline originals, protected generation, guest gallery

Validation in progress on branch feat/offline-secure-gallery, version 0.3.1.

- The real Supabase migration was applied and its transactional test rolled back all fixtures. Counter: 0 before and after. Security advisors: no warnings/errors.
- Automated OpenAI calls: 0. Gallery/print evidence uses generated calibration charts and local effects, not guest photographs.
- Chromium desktop/mobile before/after captures will be attached from Windows CI and reviewed before merge.
- Offline UI checks explicitly configure cloud credentials and inject a network failure. This is software evidence, not a physical Canon/DS620 test.
- GitHub private repository and photo bucket remain private. Only static gallery code/assets are deployed publicly.
