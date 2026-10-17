import type { Resource } from "@gd-kirie/tray";
import { defineInvokeEventa } from "@moeru/eventa";

export const bootstrap = defineInvokeEventa<Resource, Record<string, never>>("example:tray:bootstrap");

// Used only by the desktop probe to inject synthetic global signals.
export const probeEvents = defineInvokeEventa<boolean, Record<string, never>>("example:tray:probe-events");
