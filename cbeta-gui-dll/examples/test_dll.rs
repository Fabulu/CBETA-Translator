//! Test example for the CBETA GUI DLL
//! 
//! This demonstrates how to use the DLL from Rust code,
//! which is similar to how it would be used from C#.

use std::ffi::{CString, CStr};
use std::ptr;

extern "C" {
    fn translate_chinese_to_english(
        chinese_text: *const i8,
        out_english: *mut *mut i8,
    ) -> i32;
    
    fn generate_bilingual_pdf_ffi(
        chinese_sections: *const *const i8,
        english_sections: *const *const i8,
        section_count: usize,
        output_path: *const i8,
    ) -> i32;
    
    fn free_string(ptr: *mut i8);
}

fn main() -> Result<(), Box<dyn std::error::Error>> {
    println!("🧪 Testing CBETA GUI DLL...");
    
    // Test 1: Translation
    println!("\n📝 Test 1: Chinese to English Translation");
    let chinese_text = "这是一个测试。";
    let chinese_cstr = CString::new(chinese_text)?;
    
    let mut english_ptr: *mut i8 = ptr::null_mut();
    let result = unsafe {
        translate_chinese_to_english(chinese_cstr.as_ptr(), &mut english_ptr)
    };
    
    if result == 0 && !english_ptr.is_null() {
        let english_text = unsafe {
            CStr::from_ptr(english_ptr).to_string_lossy()
        };
        println!("✅ Chinese: {}", chinese_text);
        println!("✅ English: {}", english_text);
        unsafe { free_string(english_ptr); }
    } else {
        println!("❌ Translation failed with code: {}", result);
    }
    
    // Test 2: PDF Generation
    println!("\n📄 Test 2: Bilingual PDF Generation");
    let chinese_sections = vec![
        "佛說阿彌陀經".to_string(),
        "如是我聞。一時佛在舍衛國祇樹給孤獨園。".to_string(),
    ];
    
    let english_sections = vec![
        "The Amitabha Sutra".to_string(),
        "Thus have I heard. At one time the Buddha was staying in the Jeta Grove of Anathapindika's park in Sravasti.".to_string(),
    ];
    
    // Convert to C strings
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
    
    let output_path = CString::new("test_dll_output.pdf")?;
    
    let result = unsafe {
        generate_bilingual_pdf_ffi(
            chinese_ptrs.as_ptr(),
            english_ptrs.as_ptr(),
            chinese_sections.len(),
            output_path.as_ptr(),
        )
    };
    
    if result == 0 {
        println!("✅ PDF generated successfully: test_dll_output.pdf");
    } else {
        println!("❌ PDF generation failed with code: {}", result);
    }
    
    println!("\n🎉 DLL test complete!");
    Ok(())
}
