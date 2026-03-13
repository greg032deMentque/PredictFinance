export const CLIENT_DEFAULT_PATTERN = 'DOUBLE_TOP';
export const CLIENT_SUPPORTED_PATTERNS = [CLIENT_DEFAULT_PATTERN] as const;

export type ClientSupportedPattern = (typeof CLIENT_SUPPORTED_PATTERNS)[number];
