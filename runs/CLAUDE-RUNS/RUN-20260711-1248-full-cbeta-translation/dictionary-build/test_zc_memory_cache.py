import unittest
from collections import OrderedDict

import zc


class MemoryCacheTests(unittest.TestCase):
    def setUp(self):
        self.cache = zc._cache
        self.limit = zc._MEMORY_CACHE_FILES
        zc._cache = {"files": OrderedDict()}
        zc._MEMORY_CACHE_FILES = 2

    def tearDown(self):
        zc._cache = self.cache
        zc._MEMORY_CACHE_FILES = self.limit

    def test_remember_file_evicts_least_recently_used_payload(self):
        zc._remember_file("a", ("A", None))
        zc._remember_file("b", ("B", None))
        # Refresh a exactly as _load does on a cache hit.
        payload = zc._cache["files"]["a"]
        zc._cache["files"].move_to_end("a")
        self.assertEqual(("A", None), payload)
        zc._remember_file("c", ("C", None))
        self.assertEqual(["a", "c"], list(zc._cache["files"]))
        self.assertNotIn("b", zc._cache["files"])


if __name__ == "__main__":
    unittest.main()
