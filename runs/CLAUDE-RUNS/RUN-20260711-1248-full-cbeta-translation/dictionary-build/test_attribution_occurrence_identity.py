import unittest

from attribution_packet import packet


class OccurrenceIdentityIntegrationTests(unittest.TestCase):
    def test_cross_paragraph_kwic_uses_selected_lb_not_first_source_match(self):
        kwic = (
            "喜禪師杭州文喜禪師僧問如何是涅槃相師曰香煙盡處驗問如何是自己"
            "師默然僧罔措再問師曰青天蒙昧不向月邊飛袁州光涌禪師仰山南塔"
            "光涌禪師僧問文殊是七佛"
        )
        result = packet("B/B14/B14n0082.xml", "0226b04", kwic)
        self.assertEqual(result["occurrenceIdentityStatus"], "unique-kwic-fromlb")
        self.assertTrue(result["storedKwicOffsetBound"])
        self.assertEqual(result["unitType"], "paragraph-span")
        self.assertLess(len(result["caseText"]), 1000)
        self.assertIn(kwic, result["caseText"])


if __name__ == "__main__":
    unittest.main()
