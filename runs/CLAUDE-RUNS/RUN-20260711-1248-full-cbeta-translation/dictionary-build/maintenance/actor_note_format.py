"""Closed format for source notes that must name the exact canonical actor."""


def format_actor_note(rel_path: str, source_title: str, master_name: str, grammar: str) -> str:
    if not all(
        isinstance(value, str) and value.strip()
        for value in (rel_path, source_title, master_name, grammar)
    ):
        raise ValueError("actor note requires path, English title, canonical MasterName, and grammar")
    return (
        f"Source record ({rel_path}). {source_title}. "
        f"Exact actor: {master_name}. {grammar}"
    )
