//! CBETA GUI DLL
//!
//! PDF-only FFI bridge for CBETA GUI applications.

use std::ffi::CStr;
use std::os::raw::{c_char, c_int};

/// PDF layout mode.
const LAYOUT_ALTERNATING: c_int = 0;
const LAYOUT_SIDE_BY_SIDE: c_int = 1;

/// Generate a PDF from Chinese/English paragraph arrays.
///
/// layout_mode:
/// - 0: alternating Chinese then English paragraphs
/// - 1: side-by-side rows (combined into one row paragraph)
#[no_mangle]
pub extern "C" fn generate_pdf_output_ffi(
    chinese_sections: *const *const c_char,
    english_sections: *const *const c_char,
    section_count: usize,
    output_path: *const c_char,
    layout_mode: c_int,
    line_spacing: f32,
    tracking_chinese: f32,
    tracking_english: f32,
    paragraph_spacing: f32,
    auto_scale_fonts: c_int,
    target_fill_ratio: f32,
    min_font_size: f32,
    max_font_size: f32,
    lock_bilingual_font_size: c_int,
) -> c_int {
    if chinese_sections.is_null() || english_sections.is_null() || output_path.is_null() {
        return -1;
    }

    let chinese_sections = unsafe {
        std::slice::from_raw_parts(chinese_sections, section_count)
            .iter()
            .map(|&ptr| {
                if ptr.is_null() {
                    return String::new();
                }
                CStr::from_ptr(ptr).to_string_lossy().into_owned()
            })
            .collect::<Vec<_>>()
    };

    let english_sections = unsafe {
        std::slice::from_raw_parts(english_sections, section_count)
            .iter()
            .map(|&ptr| {
                if ptr.is_null() {
                    return String::new();
                }
                CStr::from_ptr(ptr).to_string_lossy().into_owned()
            })
            .collect::<Vec<_>>()
    };

    let output_path = unsafe { CStr::from_ptr(output_path).to_string_lossy().into_owned() };

    let mut font_context = match cbeta_pdf_creator::fonts::initialize_fonts() {
        Ok(fc) => fc,
        Err(e) => {
            eprintln!("Font initialization failed: {}", e);
            return -1;
        }
    };

    let include_english = english_sections.iter().any(|s| !s.trim().is_empty());
    let force_same_size = lock_bilingual_font_size != 0;

    let mut zh_size = if include_english { 12.0_f32 } else { 13.0_f32 };
    let mut en_size = if include_english { 12.0_f32 } else { 11.0_f32 };

    if force_same_size {
        let same = zh_size.min(en_size);
        zh_size = same;
        en_size = same;
    }

    if auto_scale_fonts != 0 {
        let min_size = min_font_size.max(7.0);
        let max_size = max_font_size.max(min_size);
        let clamped_target = target_fill_ratio.clamp(0.60, 0.98);
        let best = choose_auto_font_size(
            &mut font_context,
            &chinese_sections,
            &english_sections,
            layout_mode,
            line_spacing,
            paragraph_spacing,
            min_size,
            max_size,
            clamped_target,
            include_english,
            force_same_size,
        );
        zh_size = best.0;
        en_size = best.1;
    }

    font_context.set_options(
        595.0,
        842.0,
        72.0,
        zh_size,
        en_size,
        line_spacing,
        tracking_chinese,
        tracking_english,
        paragraph_spacing,
    );

    let result = match layout_mode {
        LAYOUT_SIDE_BY_SIDE => cbeta_pdf_creator::bilingual_generator::create_bilingual_pdf_side_by_side_with_context(
            &chinese_sections,
            &english_sections,
            &output_path,
            &font_context,
        ),
        LAYOUT_ALTERNATING | _ => cbeta_pdf_creator::bilingual_generator::create_bilingual_pdf_with_context(
            &chinese_sections,
            &english_sections,
            &output_path,
            &font_context,
        ),
    };

    match result {
        Ok(_) => 0,
        Err(e) => {
            eprintln!("PDF generation failed: {}", e);
            -1
        }
    }
}

fn choose_auto_font_size(
    font_context: &mut cbeta_pdf_creator::fonts::FontContext,
    chinese_sections: &[String],
    english_sections: &[String],
    layout_mode: c_int,
    line_spacing: f32,
    paragraph_spacing: f32,
    min_size: f32,
    max_size: f32,
    target_fill: f32,
    include_english: bool,
    lock_bilingual_font_size: bool,
) -> (f32, f32) {
    let mut best_zh = min_size;
    let mut best_en = if lock_bilingual_font_size { min_size } else { (min_size - 0.5).max(7.0) };
    let mut best_score = f32::MAX;
    let tracking_chinese = font_context.tracking_chinese;
    let tracking_english = font_context.tracking_english;

    let steps = 30;
    for step in 0..=steps {
        let t = step as f32 / steps as f32;
        let candidate_zh = min_size + (max_size - min_size) * t;
        let candidate_en = if lock_bilingual_font_size {
            candidate_zh
        } else {
            (candidate_zh - if include_english { 0.0 } else { 1.0 }).max(7.0)
        };

        font_context.set_options(
            595.0,
            842.0,
            72.0,
            candidate_zh,
            candidate_en,
            line_spacing,
            tracking_chinese,
            tracking_english,
            paragraph_spacing,
        );

        let fill = estimate_fill_ratio(font_context, chinese_sections, english_sections, layout_mode);
        let score = (fill - target_fill).abs();

        if score < best_score {
            best_score = score;
            best_zh = candidate_zh;
            best_en = candidate_en;
        }
    }

    (best_zh, best_en)
}

fn estimate_fill_ratio(
    font_context: &mut cbeta_pdf_creator::fonts::FontContext,
    chinese_sections: &[String],
    english_sections: &[String],
    layout_mode: c_int,
) -> f32 {
    const SAFE_INSET_X: f32 = 10.0;
    const SAFE_INSET_Y: f32 = 10.0;
    let (_content_x, _content_y, raw_content_width, raw_content_height) = font_context.content_area();
    let content_width = (raw_content_width - 2.0 * SAFE_INSET_X).max(120.0);
    let content_height = (raw_content_height - 2.0 * SAFE_INSET_Y).max(120.0);
    let left_col_width = ((content_width - 24.0) / 2.0).max(120.0);

    let row_gap = (font_context.get_line_height(true) * font_context.paragraph_spacing.max(0.2)).max(4.0);
    let section_gap = row_gap * 0.5;

    let mut total_height = 0.0_f32;
    let section_count = chinese_sections.len().max(english_sections.len());
    for i in 0..section_count {
        let zh = chinese_sections.get(i).map_or("", |s| s.as_str());
        let en = english_sections.get(i).map_or("", |s| s.as_str());

        if layout_mode == LAYOUT_SIDE_BY_SIDE {
            let zh_h = estimate_paragraph_height(font_context, zh, left_col_width, true);
            let en_h = estimate_paragraph_height(font_context, en, left_col_width, false);
            total_height += zh_h.max(en_h).max(font_context.get_line_height(true)) + row_gap;
        } else {
            if !zh.trim().is_empty() {
                total_height += estimate_paragraph_height(font_context, zh, content_width, true) + row_gap;
            }
            if !en.trim().is_empty() {
                total_height += estimate_paragraph_height(font_context, en, content_width, false) + row_gap;
            }
            total_height += section_gap;
        }
    }

    if total_height <= 1.0 {
        return 1.0;
    }

    let pages = (total_height / content_height).ceil().max(1.0);
    (total_height / (pages * content_height)).clamp(0.0, 1.0)
}

fn estimate_paragraph_height(
    font_context: &mut cbeta_pdf_creator::fonts::FontContext,
    text: &str,
    max_width: f32,
    is_chinese: bool,
) -> f32 {
    if text.trim().is_empty() {
        return 0.0;
    }

    let line_height = font_context.get_line_height(is_chinese);
    let para_gap = line_height * font_context.paragraph_spacing.max(0.0);

    let mut lines = 0.0_f32;
    for logical in text.lines() {
        let line = logical.trim();
        if line.is_empty() {
            continue;
        }
        let width = font_context.calculate_text_width(line, is_chinese);
        lines += (width / max_width.max(1.0)).ceil().max(1.0);
    }

    if lines <= 0.0 {
        lines = 1.0;
    }

    lines * line_height + para_gap
}
