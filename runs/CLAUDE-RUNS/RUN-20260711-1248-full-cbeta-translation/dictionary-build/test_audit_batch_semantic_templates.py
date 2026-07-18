import unittest

from audit_batch_semantic_templates import normalize, structural_stock


class StructuralSemanticStockTests(unittest.TestCase):
    def setUp(self):
        self.entry = {"SourceTerm": "迷逢達磨"}
        self.sense = {
            "PreferredTarget": "to meet Bodhidharma while deluded",
            "SearchAliases": ["to meet Bodhidharma while deluded"],
        }

    def stock(self, text):
        return structural_stock(normalize(text, self.entry, self.sense))

    def test_unique_descriptor_does_not_hide_empty_reader_opening(self):
        self.assertTrue(self.stock(
            "The deluded-encounter cases support the literal target to meet Bodhidharma while deluded."
        ))

    def test_unique_descriptor_does_not_hide_witness_count_filler(self):
        self.assertTrue(self.stock(
            "Six work-distinct witnesses preserve the deluded-encounter construction across its attested turns and copies."
        ))

    def test_corpus_specific_reader_prose_passes(self):
        self.assertFalse(self.stock(
            "Tianyi asks how many break through the pass yet still meet Bodhidharma in delusion; Yantou uses the phrase as a reply to a monk."
        ))


if __name__ == "__main__":
    unittest.main()
