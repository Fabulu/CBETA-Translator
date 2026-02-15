//! CBETA Bilingual PDF Creator
//! 
//! Creates high-quality bilingual PDFs with alternating Chinese/English paragraphs,
//! professional typography using fontdue, and hOCR layers for text accessibility.

pub mod accumulator;
pub mod bilingual_generator;
pub mod typography;
pub mod hocr_layer;
pub mod fonts;

// Re-export commonly used functions and types
pub use fonts::FontContext;
pub use bilingual_generator::{
    create_bilingual_pdf,
    create_bilingual_pdf_with_context,
    create_bilingual_pdf_side_by_side_with_context,
};

use anyhow::Result;
use std::ffi::{CStr, c_void};
use std::os::raw::{c_char, c_int};

/// Main entry point for bilingual PDF generation
#[no_mangle]
pub extern "C" fn generate_bilingual_pdf(
    chinese_sections: *const *const c_char,
    english_sections: *const *const c_char,
    section_count: usize,
    output_path: *const c_char,
) -> c_int {
    // Safety: Convert C strings to Rust strings
    let chinese_sections = unsafe {
        std::slice::from_raw_parts(chinese_sections, section_count)
            .iter()
            .map(|&ptr| CStr::from_ptr(ptr).to_string_lossy().into_owned())
            .collect::<Vec<_>>()
    };
    
    let english_sections = unsafe {
        std::slice::from_raw_parts(english_sections, section_count)
            .iter()
            .map(|&ptr| CStr::from_ptr(ptr).to_string_lossy().into_owned())
            .collect::<Vec<_>>()
    };
    
    let output_path = unsafe { 
        CStr::from_ptr(output_path).to_string_lossy().into_owned() 
    };

    // Generate the bilingual PDF
    match bilingual_generator::create_bilingual_pdf(
        &chinese_sections,
        &english_sections,
        &output_path,
    ) {
        Ok(_) => 0, // Success
        Err(e) => {
            eprintln!("PDF generation failed: {}", e);
            -1 // Error
        }
    }
}

/// Initialize the PDF creator (load fonts, etc.)
#[no_mangle]
pub extern "C" fn init_pdf_creator() -> *mut c_void {
    match fonts::initialize_fonts() {
        Ok(font_context) => Box::into_raw(Box::new(font_context)) as *mut c_void,
        Err(e) => {
            eprintln!("Font initialization failed: {}", e);
            std::ptr::null_mut()
        }
    }
}

/// Cleanup PDF creator resources
#[no_mangle]
pub extern "C" fn cleanup_pdf_creator(context: *mut c_void) {
    if !context.is_null() {
        unsafe {
            let _ = Box::from_raw(context as *mut fonts::FontContext);
        }
    }
}

/// Set PDF generation options with professional typography controls
#[no_mangle]
pub extern "C" fn set_pdf_options(
    context: *mut c_void,
    page_width: f32,
    page_height: f32,
    margin: f32,
    font_size_chinese: f32,
    font_size_english: f32,
    line_spacing: f32,
    tracking_chinese: f32,    // ← new
    tracking_english: f32,    // ← new
    paragraph_spacing: f32,   // ← new
) -> c_int {
    if context.is_null() {
        return -1;
    }
    
    let font_context = unsafe { &mut *(context as *mut fonts::FontContext) };
    font_context.set_options(
        page_width,
        page_height,
        margin,
        font_size_chinese,
        font_size_english,
        line_spacing,
        tracking_chinese,
        tracking_english,
        paragraph_spacing,
    );
    
    0 // Success
}
