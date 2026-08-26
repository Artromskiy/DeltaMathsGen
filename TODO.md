# DeltaMathsGen TODO

- Keep generator and DeltaMaths changes in one bounded contract update.
- Make the producer workflow build DeltaMathsGen, generate twice with no second
  diff, validate `shader-contract.json` layout/schema, then hand off to both
  DeltaMaths target builds/tests.
- Remove DeltaText implementation detail from `ARCHITECTURE.md`; link to the
  DeltaText owner instead.
