import { registerOTel } from "@vercel/otel";

function configureAspireOtlp() {
  const httpEndpoint = process.env.DOTNET_DASHBOARD_OTLP_HTTP_ENDPOINT_URL;
  if (httpEndpoint) {
    process.env.OTEL_EXPORTER_OTLP_ENDPOINT = httpEndpoint;
    process.env.OTEL_EXPORTER_OTLP_PROTOCOL = "http/protobuf";
  }
}

export function register() {
  configureAspireOtlp();
  registerOTel({
    serviceName: process.env.OTEL_SERVICE_NAME || "company-intel-ui",
  });
}
