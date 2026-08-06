import requests
import urllib3

from config import SapConfig


class SapServiceLayerClient:
    """Cliente mínimo para el Service Layer (API REST) de SAP Business One."""

    def __init__(self, config: SapConfig):
        self._config = config
        self._session = requests.Session()
        self._session.verify = config.verify_ssl
        if not config.verify_ssl:
            urllib3.disable_warnings(urllib3.exceptions.InsecureRequestWarning)

    def __enter__(self) -> "SapServiceLayerClient":
        self.login()
        return self

    def __exit__(self, exc_type, exc, tb) -> None:
        self.logout()

    def login(self) -> None:
        response = self._session.post(
            f"{self._config.base_url}/Login",
            json={
                "CompanyDB": self._config.company_db,
                "UserName": self._config.user_name,
                "Password": self._config.password,
            },
            timeout=self._config.timeout_seconds,
        )
        response.raise_for_status()

    def logout(self) -> None:
        try:
            self._session.post(
                f"{self._config.base_url}/Logout",
                timeout=self._config.timeout_seconds,
            )
        except requests.RequestException:
            pass

    def get(self, resource_path: str) -> dict:
        """resource_path ej: 'BusinessPartners?$top=10&$select=CardCode,CardName'"""
        response = self._session.get(
            f"{self._config.base_url}/{resource_path.lstrip('/')}",
            timeout=self._config.timeout_seconds,
        )
        response.raise_for_status()
        return response.json()

    def post(self, resource_path: str, payload: dict) -> dict:
        response = self._session.post(
            f"{self._config.base_url}/{resource_path.lstrip('/')}",
            json=payload,
            timeout=self._config.timeout_seconds,
        )
        response.raise_for_status()
        return response.json() if response.content else {}
