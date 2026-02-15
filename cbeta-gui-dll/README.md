# CBETA GUI DLL

PDF-only Rust DLL for CBETA GUI integration.

## Exported API

```c
int generate_pdf_output_ffi(
    const char** chinese_sections,
    const char** english_sections,
    size_t section_count,
    const char* output_path,
    int layout_mode,
    float line_spacing,
    float tracking_chinese,
    float tracking_english,
    float paragraph_spacing,
    int auto_scale_fonts,
    float target_fill_ratio,
    float min_font_size,
    float max_font_size,
    int lock_bilingual_font_size
);
```

`layout_mode`:
- `0` alternating paragraphs (Chinese, English, ...)
- `1` side-by-side rows

Returns `0` on success, `-1` on error.

Additional options:
- `auto_scale_fonts`: `0` disabled, `1` enabled
- `target_fill_ratio`: page fill target (recommended `0.85` to `0.92`)
- `min_font_size`, `max_font_size`: clamp range for auto-scaling
- `lock_bilingual_font_size`: `0` independent sizes, `1` same Chinese/English size

## Build

```bash
cargo build --release
```

Output DLL:
- `target/release/cbeta_gui_dll.dll`
