import json
from dataclasses import dataclass
from pathlib import Path

CONFIG_PATH = Path(__file__).parent / "config.json"


@dataclass
class SapConfig:
    service_layer_url: str
    company_db: str
    user_name: str
    password: str
    verify_ssl: bool = False
    timeout_seconds: int = 15

    @property
    def base_url(self) -> str:
        return self.service_layer_url.rstrip("/")


def load_config(path: Path = CONFIG_PATH) -> SapConfig:
    if not path.exists():
        raise FileNotFoundError(
            f"No se encontró {path}. Copia config.example.json a config.json y "
            "completa tus datos reales."
        )

    with open(path, encoding="utf-8") as f:
        data = json.load(f)

    return SapConfig(
        service_layer_url=data["ServiceLayerUrl"],
        company_db=data["CompanyDB"],
        user_name=data["UserName"],
        password=data["Password"],
        verify_ssl=data.get("VerifySSL", False),
        timeout_seconds=data.get("TimeoutSeconds", 15),
    )
