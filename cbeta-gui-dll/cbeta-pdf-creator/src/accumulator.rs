//! Legacy accumulator module - kept for compatibility
//!
//! This module contains the original PDF generation code that can be used as reference
//! or for compatibility with existing systems. The new bilingual generator is in
//! bilingual_generator.rs.

pub use crate::bilingual_generator::*;
pub use crate::fonts::*;
pub use crate::typography::*;
pub use crate::hocr_layer::*;

// Re-export main functionality for backward compatibility
pub use super::{generate_bilingual_pdf, init_pdf_creator, cleanup_pdf_creator, set_pdf_options};
