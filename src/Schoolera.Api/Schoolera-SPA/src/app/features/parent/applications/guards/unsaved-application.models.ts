export interface HasUnsavedApplicationChanges {
  hasUnsavedChanges(): boolean;
}

export function componentHasUnsavedApplicationChanges(
  component: unknown,
): component is HasUnsavedApplicationChanges {
  return (
    !!component &&
    typeof (component as HasUnsavedApplicationChanges).hasUnsavedChanges === 'function'
  );
}
