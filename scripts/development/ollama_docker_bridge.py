#!/usr/bin/env python3
"""Ponte opcional de desenvolvimento: Docker -> Ollama no loopback do Fedora.

Escuta somente no gateway privado do Docker, sem alterar o serviço Ollama.
Uso: python3 scripts/development/ollama_docker_bridge.py --bind 172.17.0.1
"""
import argparse
import http.server
import urllib.error
import urllib.request


class Handler(http.server.BaseHTTPRequestHandler):
    def proxy(self):
        allowed = {("GET", "/api/tags"), ("POST", "/api/chat")}
        if (self.command, self.path) not in allowed:
            self.send_error(404)
            return
        try:
            length = int(self.headers.get("Content-Length", "0"))
        except ValueError:
            self.send_error(400)
            return
        if length < 0 or length > 16 * 1024 * 1024:
            self.send_error(413)
            return
        payload = self.rfile.read(length) if self.command == "POST" else None
        request = urllib.request.Request("http://127.0.0.1:11434" + self.path, data=payload,
                                         headers={"Content-Type": "application/json"}, method=self.command)
        try:
            with urllib.request.urlopen(request, timeout=120) as response:
                body = response.read()
                self.send_response(response.status)
                self.send_header("Content-Type", "application/json")
                self.send_header("Content-Length", str(len(body)))
                self.end_headers()
                self.wfile.write(body)
        except urllib.error.HTTPError as error:
            self.send_error(error.code)
        except (OSError, TimeoutError):
            self.send_error(502)

    do_GET = proxy
    do_POST = proxy

    def log_message(self, *args):
        pass  # Não registrar conteúdo de conversas.


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--bind", required=True)
    args = parser.parse_args()
    import ipaddress
    address = ipaddress.ip_address(args.bind)
    if not address.is_private or address.is_unspecified or address.is_loopback:
        parser.error("Informe somente o IP privado do gateway Docker.")
    http.server.ThreadingHTTPServer((args.bind, 11434), Handler).serve_forever()
