"""
Creates Farm Fury: Stampede's in-app purchases in App Store Connect through the App Store Connect API.

For each product it creates (or, when it already exists, completes) the IAP itself, its en-US name/description,
its USD base price (Apple fills in the other countries) and its availability in every territory. Run it again at
any time: existing products are left as they are and only missing parts are added. It never deletes anything.

The catalog below must match Assets/_Project/Scripts/Core/Store.cs (StoreProducts) and IAPManager's prices;
the store ids are StoreProducts.StoreIdPrefix ("stampede_") + the game's id.

Not done here (App Store Connect web page): the review screenshot each product needs before submission, and
submitting the products with the first app version.

Needs: Python 3.9+ and the 'cryptography' package. An App Store Connect API team key with the App Manager (or
Admin) role: Users and Access > Integrations > App Store Connect API (Key ID, Issuer ID, the .p8 file).
Never commit the .p8.

    python create_iap_products.py --key-id ABC123 --issuer-id 69a6de7e-... --key-file C:/path/AuthKey_ABC123.p8 --dry-run
    python create_iap_products.py --key-id ABC123 --issuer-id 69a6de7e-... --key-file C:/path/AuthKey_ABC123.p8
"""

import argparse
import base64
import json
import sys
import time
import urllib.error
import urllib.parse
import urllib.request

from cryptography.hazmat.primitives import hashes, serialization
from cryptography.hazmat.primitives.asymmetric import ec
from cryptography.hazmat.primitives.asymmetric.utils import decode_dss_signature

API = "https://api.appstoreconnect.apple.com"
BUNDLE_ID = "com.farmfury.stampede"
PREFIX = "stampede_"
REVIEW_NOTE = ("Bought in the in-game Shop (landing screen settings cog > Shop), behind a parental gate. "
               "Restore Purchases is on the Cash page.")

C, N = "CONSUMABLE", "NON_CONSUMABLE"

# (game id, type, USD price, display name <= 30 chars, description <= 45 chars)
PRODUCTS = [
    ("remove_ads", N, "4.99", "Remove Ads", "No more ads, plus 100 bonus coins"),
    ("coins_100", C, "0.99", "100 Coins", "A pouch of 100 coins"),
    ("coins_500", C, "3.99", "500 Coins", "A sack of 500 coins"),
    ("coins_5000", C, "9.99", "5,000 Coins", "A cart full of 5,000 coins"),
    ("coins_15000", C, "19.99", "15,000 Coins", "A barn full of 15,000 coins"),
    ("hat_sombrero", N, "1.99", "Sombrero", "A wide fiesta hat for your animals"),
    ("hat_baseball_cap", N, "1.99", "Baseball Cap", "A sporty cap in each animal's colour"),
    ("hat_cowboy_hat", N, "1.99", "Cowboy Hat", "Yeehaw! Round up robots in style"),
    ("hat_chef_hat", N, "1.99", "Chef Hat", "A crisp chef's hat for your animals"),
    ("hat_crown", N, "1.99", "Crown", "Be king or queen of the farm"),
    ("trail_rainbowribbon", N, "1.99", "Rainbow Ribbon", "A rainbow trail follows every step"),
    ("trail_sparkledust", N, "1.99", "Sparkle Dust", "A trail of magic sparkles"),
    ("trail_cornhusk", N, "1.99", "Corn Husk Trail", "A rustling trail of golden corn husks"),
    ("trail_ember", N, "1.99", "Ember Trail", "A trail of glowing embers"),
    ("trail_confetti", N, "1.99", "Confetti Trail", "Colourful confetti with every step"),
    ("trail_bubbles", N, "1.99", "Bubbles Trail", "A trail of floating bubbles"),
    ("machine_tractor_clucky", N, "3.99", "Cluck's Tractor", "Cluck zooms around on her own tractor"),
    ("machine_truck_bessie", N, "3.99", "Bessie's Milk Tanker", "Bessie rolls out in a milk tanker"),
    ("machine_hay_horace", N, "3.99", "Horace's Hay Baler", "Horace rides a speedy hay baler"),
    ("world_dustbowlcanyon", N, "3.99", "Dustbowl Canyon", "12 new levels in a dusty canyon"),
    ("world_harvestfairground", N, "3.99", "Harvest Fairground", "12 new levels at the night fair"),
    ("world_cropfactory", N, "3.99", "Crop Factory", "12 levels in the robots' factory"),
]


def b64url(data: bytes) -> str:
    return base64.urlsafe_b64encode(data).rstrip(b"=").decode()


class Api:
    def __init__(self, key_id, issuer_id, key_file):
        with open(key_file, "rb") as f:
            self.key = serialization.load_pem_private_key(f.read(), password=None)
        self.key_id, self.issuer_id = key_id, issuer_id
        self.token, self.token_exp = None, 0

    def _token(self):
        now = int(time.time())
        if self.token and now < self.token_exp - 60:
            return self.token
        header = {"alg": "ES256", "kid": self.key_id, "typ": "JWT"}
        payload = {"iss": self.issuer_id, "iat": now, "exp": now + 1200, "aud": "appstoreconnect-v1"}
        signing_input = b64url(json.dumps(header).encode()) + "." + b64url(json.dumps(payload).encode())
        r, s = decode_dss_signature(self.key.sign(signing_input.encode(), ec.ECDSA(hashes.SHA256())))
        self.token = signing_input + "." + b64url(r.to_bytes(32, "big") + s.to_bytes(32, "big"))
        self.token_exp = now + 1200
        return self.token

    def call(self, method, path, body=None, allow_404=False, attempt=0):
        url = path if path.startswith("http") else API + path
        data = json.dumps(body).encode() if body is not None else None
        req = urllib.request.Request(url, data=data, method=method, headers={
            "Authorization": "Bearer " + self._token(), "Content-Type": "application/json"})
        try:
            with urllib.request.urlopen(req) as resp:
                text = resp.read().decode()
                return json.loads(text) if text else {}
        except urllib.error.HTTPError as e:
            if allow_404 and e.code == 404:
                return None
            # Apple's API often answers a valid request with a passing 500/503 or rate-limits with 429.
            if e.code in (429, 500, 502, 503, 504) and method == "GET" and attempt < 4:
                time.sleep(5 * (attempt + 1))
                return self.call(method, path, body, allow_404, attempt + 1)
            detail = e.read().decode()
            try:
                errors = json.loads(detail).get("errors", [])
                detail = "; ".join(f"{x.get('title')}: {x.get('detail')}" for x in errors) or detail
            except ValueError:
                pass
            raise RuntimeError(f"{method} {path} -> HTTP {e.code}: {detail}") from None

    def get_all(self, path):
        """Follows links.next and returns every item of a list endpoint."""
        items, url = [], path
        while url:
            page = self.call("GET", url)
            items.extend(page.get("data", []))
            url = page.get("links", {}).get("next")
        return items


def find_app(api, bundle_id):
    apps = api.call("GET", "/v1/apps?" + urllib.parse.urlencode({"filter[bundleId]": bundle_id}))["data"]
    if not apps:
        sys.exit(f"No app with bundle id {bundle_id} in this App Store Connect account.")
    return apps[0]["id"]


def create_iap(api, app_id, store_id, kind, name, family):
    attrs = {"name": name, "productId": store_id, "inAppPurchaseType": kind, "reviewNote": REVIEW_NOTE}
    if family and kind == N:
        attrs["familySharable"] = True
    body = {"data": {"type": "inAppPurchases", "attributes": attrs,
                     "relationships": {"app": {"data": {"type": "apps", "id": app_id}}}}}
    return api.call("POST", "/v2/inAppPurchases", body)["data"]["id"]


def ensure_localization(api, iap_id, name, description):
    existing = api.get_all(f"/v2/inAppPurchases/{iap_id}/inAppPurchaseLocalizations")
    if any(loc["attributes"].get("locale") == "en-US" for loc in existing):
        return False
    api.call("POST", "/v1/inAppPurchaseLocalizations", {"data": {
        "type": "inAppPurchaseLocalizations",
        "attributes": {"locale": "en-US", "name": name, "description": description},
        "relationships": {"inAppPurchaseV2": {"data": {"type": "inAppPurchases", "id": iap_id}}}}})
    return True


def ensure_price(api, iap_id, usd):
    schedule = api.call("GET", f"/v2/inAppPurchases/{iap_id}/iapPriceSchedule", allow_404=True)
    if schedule and schedule.get("data"):
        manual = api.call("GET", f"/v1/inAppPurchasePriceSchedules/{schedule['data']['id']}/manualPrices",
                          allow_404=True)
        if manual and manual.get("data"):
            return False
    points = api.get_all(f"/v2/inAppPurchases/{iap_id}/pricePoints?filter[territory]=USA&limit=200")
    point = next((p for p in points if abs(float(p["attributes"]["customerPrice"]) - float(usd)) < 0.001), None)
    if point is None:
        raise RuntimeError(f"no USD price point {usd}")
    api.call("POST", "/v1/inAppPurchasePriceSchedules", {
        "data": {"type": "inAppPurchasePriceSchedules", "relationships": {
            "inAppPurchase": {"data": {"type": "inAppPurchases", "id": iap_id}},
            "baseTerritory": {"data": {"type": "territories", "id": "USA"}},
            "manualPrices": {"data": [{"type": "inAppPurchasePrices", "id": "${price0}"}]}}},
        "included": [{"type": "inAppPurchasePrices", "id": "${price0}", "attributes": {"startDate": None},
                      "relationships": {
                          "inAppPurchaseV2": {"data": {"type": "inAppPurchases", "id": iap_id}},
                          "inAppPurchasePricePoint": {"data": {"type": "inAppPurchasePricePoints",
                                                               "id": point["id"]}}}}]})
    return True


def ensure_availability(api, iap_id, territories):
    existing = api.call("GET", f"/v2/inAppPurchases/{iap_id}/inAppPurchaseAvailability", allow_404=True)
    if existing and existing.get("data"):
        return False
    api.call("POST", "/v1/inAppPurchaseAvailabilities", {"data": {
        "type": "inAppPurchaseAvailabilities", "attributes": {"availableInNewTerritories": True},
        "relationships": {
            "inAppPurchase": {"data": {"type": "inAppPurchases", "id": iap_id}},
            "availableTerritories": {"data": [{"type": "territories", "id": t} for t in territories]}}}})
    return True


def main():
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--key-id", required=True)
    parser.add_argument("--issuer-id", required=True)
    parser.add_argument("--key-file", required=True, help="the AuthKey_XXXX.p8 file")
    parser.add_argument("--bundle-id", default=BUNDLE_ID)
    parser.add_argument("--dry-run", action="store_true", help="only list what exists and what would be created")
    parser.add_argument("--family-sharing", action="store_true",
                        help="turn Family Sharing on for new non-consumables (it can never be turned off again)")
    args = parser.parse_args()

    for gid, _, _, name, desc in PRODUCTS:
        if len(name) > 30 or len(desc) > 45:
            sys.exit(f"{gid}: name must be <= 30 and description <= 45 characters")

    api = Api(args.key_id, args.issuer_id, args.key_file)
    app_id = find_app(api, args.bundle_id)
    print(f"App {args.bundle_id} = {app_id}")

    existing = {iap["attributes"]["productId"]: iap["id"]
                for iap in api.get_all(f"/v1/apps/{app_id}/inAppPurchasesV2?limit=200")}
    territories = None if args.dry_run else [t["id"] for t in api.get_all("/v1/territories?limit=200")]

    failures = 0
    for gid, kind, usd, name, desc in PRODUCTS:
        store_id = PREFIX + gid
        iap_id = existing.get(store_id)
        if args.dry_run:
            print(f"  {'exists ' if iap_id else 'create '} {store_id:36} {kind:15} ${usd:>6}  {name}")
            continue
        try:
            done = []
            if iap_id is None:
                iap_id = create_iap(api, app_id, store_id, kind, name, args.family_sharing)
                done.append("created")
            if ensure_localization(api, iap_id, name, desc):
                done.append("text")
            if ensure_price(api, iap_id, usd):
                done.append(f"${usd}")
            if ensure_availability(api, iap_id, territories):
                done.append(f"{len(territories)} territories")
            print(f"  OK   {store_id:36} {', '.join(done) or 'already complete'}")
        except RuntimeError as e:
            failures += 1
            print(f"  FAIL {store_id:36} {e}")

    if failures:
        sys.exit(f"{failures} product(s) failed; fix the cause and run again (finished parts are kept).")
    if not args.dry_run:
        print("Done. Still to do in the web page: a review screenshot per product, then submit them with the app version.")


if __name__ == "__main__":
    main()
