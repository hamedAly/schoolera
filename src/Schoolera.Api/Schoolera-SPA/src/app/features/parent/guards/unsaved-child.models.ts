export interface HasUnsavedChildChanges {
  hasUnsavedChanges(): boolean;
}

export function componentHasUnsavedChildChanges(
  component: unknown,
): component is HasUnsavedChildChanges {
  return (
    !!component &&
    typeof (component as HasUnsavedChildChanges).hasUnsavedChanges === 'function'
  );
}
