use cbeta_pdf_creator::{generate_bilingual_pdf, init_pdf_creator, set_pdf_options, cleanup_pdf_creator};
use std::ffi::CString;

fn main() {
    println!("Testing bilingual PDF generation...");

    // Initialize the PDF creator
    let context = unsafe { init_pdf_creator() };
    if context.is_null() {
        eprintln!("Failed to initialize PDF creator");
        return;
    }

    // Set PDF options
    unsafe {
        set_pdf_options(
            context,
            595.0,  // page width (A4)
            842.0,  // page height (A4)
            72.0,   // margin
            12.0,   // Chinese font size
            11.0,   // English font size
            1.4,    // line spacing
            12.0,   // Chinese tracking
            8.0,    // English tracking
            0.6,    // paragraph spacing
        );
    }

    // Create sample Chinese and English text
    let chinese_texts = vec![
        CString::new("這是測試中文段落。").unwrap(),
        CString::new("第二個中文段落。").unwrap(),
    ];
    
    let english_texts = vec![
        CString::new("This is a test English paragraph.").unwrap(),
        CString::new("Second English paragraph.").unwrap(),
    ];

    // Prepare arrays of C string pointers
    let chinese_ptrs: Vec<*const i8> = chinese_texts.iter().map(|s| s.as_ptr()).collect();
    let english_ptrs: Vec<*const i8> = english_texts.iter().map(|s| s.as_ptr()).collect();

    // Output path
    let output_path = CString::new("test_output.pdf").unwrap();

    // Generate the PDF
    let result = unsafe {
        generate_bilingual_pdf(
            chinese_ptrs.as_ptr(),
            english_ptrs.as_ptr(),
            2,  // count
            output_path.as_ptr(),
        )
    };

    if result == 0 {
        println!("Successfully generated test_output.pdf");
    } else {
        eprintln!("Failed to generate PDF");
    }

    // Clean up
    unsafe {
        cleanup_pdf_creator(context);
    }
}