#!/usr/bin/env python3
"""
Generate a readable summary report from the projectdesc analysis results.
"""

import json
from collections import defaultdict

def generate_summary_report():
    """Generate a readable summary report."""
    
    # Load the analysis results
    with open('projectdesc_analysis.json', 'r', encoding='utf-8') as f:
        results = json.load(f)
    
    summary = results['summary']
    project_groups = results['project_groups']
    
    # Create report
    report_lines = []
    report_lines.append("=" * 80)
    report_lines.append("CBETA XML ProjectDesc Analysis Report")
    report_lines.append("=" * 80)
    report_lines.append(f"Total XML files analyzed: {summary['total_files']}")
    report_lines.append(f"Unique projectDesc categories: {summary['unique_project_desc_count']}")
    report_lines.append(f"Files with errors: {summary['files_with_errors']}")
    report_lines.append("")
    
    # Group by major contributors/contributions
    contributor_groups = defaultdict(list)
    
    for project_desc, data in project_groups.items():
        file_count = data['file_count']
        
        # Categorize by major patterns
        if "CBETA" in project_desc and "Input by CBETA" in project_desc:
            contributor_groups["CBETA Direct Input"].append((project_desc, file_count))
        elif "CBETA OCR Group" in project_desc:
            contributor_groups["CBETA OCR Processing"].append((project_desc, file_count))
        elif "Christian Wittern" in project_desc:
            contributor_groups["Christian Wittern Contributions"].append((project_desc, file_count))
        elif "Tripitaka Koreana" in project_desc:
            contributor_groups["Tripitaka Koreana Related"].append((project_desc, file_count))
        elif "Punctuated text as provided by" in project_desc:
            contributor_groups["Punctuation Provided"].append((project_desc, file_count))
        elif "OCR" in project_desc:
            contributor_groups["OCR Processing"].append((project_desc, file_count))
        elif "Text as provided by" in project_desc:
            contributor_groups["Text Provided by Contributors"].append((project_desc, file_count))
        else:
            contributor_groups["Other"].append((project_desc, file_count))
    
    # Print summary by contributor groups
    report_lines.append("MAJOR CONTRIBUTOR GROUPS:")
    report_lines.append("-" * 40)
    
    total_in_groups = 0
    for group_name, items in sorted(contributor_groups.items()):
        group_total = sum(count for _, count in items)
        total_in_groups += group_total
        report_lines.append(f"{group_name}: {group_total} files in {len(items)} categories")
    
    report_lines.append("")
    report_lines.append("TOP 20 LARGEST CATEGORIES:")
    report_lines.append("-" * 40)
    
    # Sort all categories by file count
    sorted_categories = sorted(
        project_groups.items(),
        key=lambda x: x[1]['file_count'],
        reverse=True
    )
    
    for i, (project_desc, data) in enumerate(sorted_categories[:20], 1):
        file_count = data['file_count']
        # Truncate long descriptions for readability
        display_desc = project_desc[:100] + "..." if len(project_desc) > 100 else project_desc
        report_lines.append(f"{i:2d}. {file_count:4d} files: {display_desc}")
    
    report_lines.append("")
    report_lines.append("SMALL CATEGORIES (1-5 files each):")
    report_lines.append("-" * 40)
    
    small_categories = [(desc, data['file_count']) for desc, data in sorted_categories 
                       if data['file_count'] <= 5]
    
    report_lines.append(f"Categories with 1-5 files: {len(small_categories)}")
    report_lines.append(f"Total files in small categories: {sum(count for _, count in small_categories)}")
    
    # Show some examples of unique single-file categories
    single_file_categories = [(desc, data['file_count']) for desc, data in sorted_categories 
                             if data['file_count'] == 1]
    
    report_lines.append("")
    report_lines.append("EXAMPLES OF UNIQUE CATEGORIES (1 file each):")
    report_lines.append("-" * 40)
    
    for i, (project_desc, count) in enumerate(single_file_categories[:10], 1):
        display_desc = project_desc[:120] + "..." if len(project_desc) > 120 else project_desc
        report_lines.append(f"{i}. {display_desc}")
    
    if len(single_file_categories) > 10:
        report_lines.append(f"... and {len(single_file_categories) - 10} more unique categories")
    
    # Write report to file
    with open('projectdesc_report.txt', 'w', encoding='utf-8') as f:
        f.write('\n'.join(report_lines))
    
    # Print to console
    print('\n'.join(report_lines))
    print(f"\nFull report saved to: projectdesc_report.txt")

if __name__ == "__main__":
    generate_summary_report()
