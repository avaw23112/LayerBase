from __future__ import annotations

import apply_main_scope_refactor as refactor


_original_replace_once = refactor.replace_once


def replace_once_with_ordered_lifecycle_support(
    text: str,
    old: str,
    new: str,
    label: str,
) -> str:
    """Apply ordered replacements for the two structurally identical lifecycle blocks."""
    if label in {
        "extend Service start lifecycle",
        "extend Context start lifecycle",
    }:
        count = text.count(old)
        if count < 1:
            raise RuntimeError(f"{label}: expected at least one match, found {count}")
        return text.replace(old, new, 1)

    return _original_replace_once(text, old, new, label)


refactor.replace_once = replace_once_with_ordered_lifecycle_support
refactor.main()
