from __future__ import annotations

from pathlib import Path

import apply_main_scope_refactor as refactor


ROOT = Path(__file__).resolve().parents[1]
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


def fix_value_type_options() -> None:
    """The three scheduler option types are structs, so assignment needs no null guard."""
    path = ROOT / "LayerBase/Application/LayerRuntime.cs"
    text = path.read_text(encoding="utf-8")
    replacements = {
        "_postOptions = options ?? throw new ArgumentNullException(nameof(options));": "_postOptions = options;",
        "_timerOptions = options ?? throw new ArgumentNullException(nameof(options));": "_timerOptions = options;",
        "_delayOptions = options ?? throw new ArgumentNullException(nameof(options));": "_delayOptions = options;",
    }

    for old, new in replacements.items():
        count = text.count(old)
        if count != 1:
            raise RuntimeError(f"option assignment patch expected one match for {old!r}, found {count}")
        text = text.replace(old, new, 1)

    path.write_text(text, encoding="utf-8", newline="\n")


def migrate_context_tests_to_main_scope() -> None:
    """Continuation tests must enter the context now owned by MainScope."""
    path = ROOT / "LayerBase.Test/ConcurrencySimplifiedTests.cs"
    text = path.read_text(encoding="utf-8-sig")
    old = "runtime._context!.EnterScope()"
    expected = 3
    count = text.count(old)
    if count != expected:
        raise RuntimeError(f"context test migration expected {expected} matches, found {count}")

    text = text.replace(old, "runtime.MainScope.ContextForTest!.EnterScope()")
    path.write_text(text, encoding="utf-8", newline="\n")


refactor.replace_once = replace_once_with_ordered_lifecycle_support
refactor.main()
fix_value_type_options()
migrate_context_tests_to_main_scope()
