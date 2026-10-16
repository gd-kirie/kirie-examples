import type { Resource } from "@gd-kirie/tray";
import { defineInvokeEventa } from "@moeru/eventa";

export const bootstrap = defineInvokeEventa<Resource, Record<string, never>>("example:tray:bootstrap");
