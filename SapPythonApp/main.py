import json
import sys

from config import load_config
from sap_client import SapServiceLayerClient

DEFAULT_RESOURCE = "BusinessPartners?$top=10&$select=CardCode,CardName"


def main() -> int:
    resource_path = sys.argv[1] if len(sys.argv) > 1 else DEFAULT_RESOURCE

    config = load_config()
    with SapServiceLayerClient(config) as sap:
        data = sap.get(resource_path)

    print(json.dumps(data, indent=2, ensure_ascii=False))
    return 0


if __name__ == "__main__":
    sys.exit(main())
