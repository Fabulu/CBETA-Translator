#!/usr/bin/env python3
"""
CBETA XML ProjectDesc Analyzer

This script scans all XML files in the CbetaZenTexts directory,
extracts the <projectDesc> tag content from each file,
and organizes the files by their projectDesc content.
Results are saved to a JSON file with file counts per projectDesc.
"""

import os
import json
import xml.etree.ElementTree as ET
from collections import defaultdict
from pathlib import Path
import re
from typing import Dict, List, Set

def extract_project_desc(xml_file_path: str) -> str:
    """
    Extract the projectDesc content from an XML file.
    Returns the English text content of the projectDesc tag.
    """
    try:
        # Parse the XML file
        tree = ET.parse(xml_file_path)
        root = tree.getroot()
        
        # Define namespace
        ns = {'tei': 'http://www.tei-c.org/ns/1.0'}
        
        # Find the projectDesc element
        project_desc = root.find('.//tei:projectDesc', ns)
        
        if project_desc is not None:
            # Get all p elements within projectDesc
            p_elements = project_desc.findall('tei:p', ns)
            
            # Extract English text (xml:lang="en")
            english_texts = []
            for p in p_elements:
                lang = p.get('{http://www.w3.org/XML/1998/namespace}lang')
                if lang == 'en':
                    english_texts.append(p.text.strip() if p.text else '')
            
            if english_texts:
                return ' | '.join(english_texts)
            else:
                # If no English text found, get all text content
                all_texts = []
                for p in p_elements:
                    if p.text:
                        all_texts.append(p.text.strip())
                return ' | '.join(all_texts) if all_texts else "No project description found"
        
        return "No projectDesc tag found"
        
    except ET.ParseError as e:
        return f"XML parsing error: {str(e)}"
    except Exception as e:
        return f"Error reading file: {str(e)}"

def find_xml_files(base_dir: str) -> List[str]:
    """
    Find all XML files in the directory tree.
    """
    xml_files = []
    base_path = Path(base_dir)
    
    # Walk through all directories
    for xml_file in base_path.rglob('*.xml'):
        xml_files.append(str(xml_file))
    
    return sorted(xml_files)

def analyze_xml_files(xml_directory: str) -> Dict[str, Dict]:
    """
    Analyze all XML files and group them by projectDesc content.
    """
    print(f"Scanning XML files in: {xml_directory}")
    
    # Find all XML files
    xml_files = find_xml_files(xml_directory)
    print(f"Found {len(xml_files)} XML files")
    
    # Group files by projectDesc
    project_groups = defaultdict(list)
    processed_files = 0
    errors = []
    
    for xml_file in xml_files:
        processed_files += 1
        
        # Show progress
        if processed_files % 100 == 0:
            print(f"Processed {processed_files}/{len(xml_files)} files...")
        
        # Extract projectDesc
        project_desc = extract_project_desc(xml_file)
        
        # Get relative file path
        rel_path = os.path.relpath(xml_file, xml_directory)
        
        # Group by projectDesc
        project_groups[project_desc].append(rel_path)
        
        # Track errors
        if "error" in project_desc.lower() or "not found" in project_desc.lower():
            errors.append({
                'file': rel_path,
                'issue': project_desc
            })
    
    print(f"Completed processing {processed_files} files")
    
    # Convert to final format
    result = {
        'summary': {
            'total_files': len(xml_files),
            'unique_project_desc_count': len(project_groups),
            'files_with_errors': len(errors)
        },
        'project_groups': {},
        'errors': errors
    }
    
    # Add project groups with file counts
    for project_desc, files in sorted(project_groups.items()):
        result['project_groups'][project_desc] = {
            'file_count': len(files),
            'files': files
        }
    
    return result

def save_results(results: Dict[str, Dict], output_file: str):
    """
    Save the analysis results to a JSON file.
    """
    print(f"Saving results to: {output_file}")
    
    with open(output_file, 'w', encoding='utf-8') as f:
        json.dump(results, f, indent=2, ensure_ascii=False)
    
    print("Results saved successfully!")

def print_summary(results: Dict[str, Dict]):
    """
    Print a summary of the analysis results.
    """
    summary = results['summary']
    print(f"\n{'='*60}")
    print("CBETA XML ProjectDesc Analysis Summary")
    print(f"{'='*60}")
    print(f"Total XML files processed: {summary['total_files']}")
    print(f"Unique projectDesc categories: {summary['unique_project_desc_count']}")
    print(f"Files with errors: {summary['files_with_errors']}")
    
    print(f"\n{'='*60}")
    print("ProjectDesc Categories (sorted by file count)")
    print(f"{'='*60}")
    
    # Sort project groups by file count (descending)
    sorted_groups = sorted(
        results['project_groups'].items(),
        key=lambda x: x[1]['file_count'],
        reverse=True
    )
    
    for i, (project_desc, data) in enumerate(sorted_groups, 1):
        file_count = data['file_count']
        # Truncate long projectDesc for display
        display_desc = project_desc[:80] + "..." if len(project_desc) > 80 else project_desc
        print(f"{i:2d}. {file_count:4d} files: {display_desc}")
    
    if results['errors']:
        print(f"\n{'='*60}")
        print("Files with Issues")
        print(f"{'='*60}")
        for error in results['errors'][:10]:  # Show first 10 errors
            print(f"  {error['file']}: {error['issue']}")
        if len(results['errors']) > 10:
            print(f"  ... and {len(results['errors']) - 10} more errors")

def main():
    """
    Main function to run the analysis.
    """
    # Define paths
    base_dir = r"D:\Rust-projects\not-rust-projects\CBETA-Translator\CbetaZenTexts\xml-p5"
    output_file = r"D:\Rust-projects\not-rust-projects\CBETA-Translator\projectdesc_analysis.json"
    
    # Check if directory exists
    if not os.path.exists(base_dir):
        print(f"Error: Directory not found: {base_dir}")
        return
    
    # Run analysis
    print("Starting CBETA XML ProjectDesc analysis...")
    results = analyze_xml_files(base_dir)
    
    # Save results
    save_results(results, output_file)
    
    # Print summary
    print_summary(results)
    
    print(f"\nDetailed results saved to: {output_file}")
    print("You can now examine the JSON file to decide on physical sorting strategy.")

if __name__ == "__main__":
    main()
