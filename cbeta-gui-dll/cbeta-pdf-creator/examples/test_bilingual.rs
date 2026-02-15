//! Test example for bilingual PDF generation

use std::ffi::{CStr, CString};
use std::ptr;

fn main() -> Result<(), Box<dyn std::error::Error>> {
    println!("Testing CBETA Bilingual PDF Creator...");
    
    // Test data - alternating Chinese/English paragraphs
    let chinese_sections = vec![
        "佛說阿彌陀經".to_string(),
        "如是我聞。一時佛在舍衛國祇樹給孤獨園。".to_string(),
        "與大比丘僧，千二百五十人俱，皆是大阿羅漢。".to_string(),
    ];
    
    let english_sections = vec![
        "The Amitabha Sutra".to_string(),
        "Thus have I heard. At one time the Buddha was staying in the Jeta Grove of Anathapindika's park in Sravasti.".to_string(),
        "Together with a great assembly of twelve hundred and fifty monks, all of whom were great Arhats.".to_string(),
    ];
    
    // Convert to C strings for FFI testing
    let chinese_c_strings: Vec<CString> = chinese_sections.iter()
        .map(|s| CString::new(s.as_str()).unwrap())
        .collect();
    
    let english_c_strings: Vec<CString> = english_sections.iter()
        .map(|s| CString::new(s.as_str()).unwrap())
        .collect();
    
    let chinese_ptrs: Vec<*const i8> = chinese_c_strings.iter()
        .map(|cs| cs.as_ptr())
        .collect();
    
    let english_ptrs: Vec<*const i8> = english_c_strings.iter()
        .map(|cs| cs.as_ptr())
        .collect();
    
    let output_path = CString::new("test_bilingual.pdf")?;
    
    // Test the FFI function
    println!("Calling generate_bilingual_pdf...");
    let result = unsafe {
        cbeta_pdf_creator::generate_bilingual_pdf(
            chinese_ptrs.as_ptr(),
            english_ptrs.as_ptr(),
            chinese_sections.len(),
            output_path.as_ptr(),
        )
    };
    
    match result {
        0 => println!("✅ PDF generated successfully!"),
        _ => println!("❌ PDF generation failed with code: {}", result),
    }
    
    Ok(())
}
