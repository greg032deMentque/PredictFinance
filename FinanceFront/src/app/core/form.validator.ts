import { AbstractControl, FormArray, FormGroup, ValidationErrors } from "@angular/forms";

export function nonNegativeValidator(control: AbstractControl): ValidationErrors | null {
  const v = control.value;
  return v == null || v >= 0 ? null : { nonNegative: true };
}

/**
 * RÃ©cupÃ¨re rÃ©cursivement la liste dÃ©taillÃ©e des champs invalides dâ€™un `FormGroup` ou `FormArray`,
 * avec leur chemin complet (`path`) et leurs erreurs de validation.
 *
 * âš™ï¸ GÃ¨re les sous-groupes et tableaux imbriquÃ©s (`address.street`, `phones[0].number`, etc.).
 * âœ… Inclut Ã©galement les erreurs portÃ©es par les conteneurs eux-mÃªmes (validators de groupe).
 *
 * ---
 * @example
 * // Exemple de structure de formulaire :
 * this.form = this.fb.group({
 *   username: ['', Validators.required],
 *   address: this.fb.group({
 *     street: ['', Validators.required],
 *     city: ['']
 *   }),
 *   phones: this.fb.array([
 *     this.fb.group({
 *       number: ['', Validators.minLength(10)]
 *     })
 *   ])
 * });
 *
 * // Exemple d'utilisation :
 * if (this.form.invalid) {
 *   this.form.markAllAsTouched();
 *   console.table(getInvalidControlsDetailed(this.form));
 * }
 *
 * // Exemple de sortie console :
 * // â”Œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”¬â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”¬â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”
 * // â”‚ (index) â”‚ path                  â”‚ errors                     â”‚
 * // â”œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”¼â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”¼â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”¤
 * // â”‚    0    â”‚ 'username'            â”‚ { required: true }         â”‚
 * // â”‚    1    â”‚ 'address.street'      â”‚ { required: true }         â”‚
 * // â”‚    2    â”‚ 'phones[0].number'    â”‚ { minlength: { ... } }     â”‚
 * // â””â”€â”€â”€â”€â”€â”€â”€â”€â”€â”´â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”´â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”˜
 *
 * @param form        Le `FormGroup` ou `FormArray` Ã  inspecter.
 * @param parentPath  (optionnel) Chemin du parent pour la rÃ©cursion interne.
 * @returns Un tableau dâ€™objets `{ path, errors }` listant tous les contrÃ´les invalides.
 */
export function getInvalidControlsDetailed(
  form: FormGroup | FormArray,
  parentPath = ''
): { path: string; errors: ValidationErrors | null }[] {
  const out: { path: string; errors: ValidationErrors | null }[] = [];

  if (form instanceof FormGroup) {
    for (const [key, control] of Object.entries(form.controls)) {
      const path = parentPath ? `${parentPath}.${key}` : key;

      if (control instanceof FormGroup || control instanceof FormArray) {
        // Erreurs portÃ©es par le conteneur lui-mÃªme (validators de groupe)
        if (control.invalid && control.errors) {
          out.push({ path, errors: control.errors });
        }
        out.push(...getInvalidControlsDetailed(control, path));
      } else if (control.invalid) {
        out.push({ path, errors: control.errors });
      }
    }
  } else if (form instanceof FormArray) {
    form.controls.forEach((control: AbstractControl, index: number) => {
      const path = parentPath ? `${parentPath}[${index}]` : `[${index}]`;

      if (control instanceof FormGroup || control instanceof FormArray) {
        if (control.invalid && control.errors) {
          out.push({ path, errors: control.errors });
        }
        out.push(...getInvalidControlsDetailed(control, path));
      } else if (control.invalid) {
        out.push({ path, errors: control.errors });
      }
    });
  }

  return out;
}

export function getInvalidControls(
  form: FormGroup | FormArray,
  parentPath = ''
): string[] {
  const invalid: string[] = [];

  if (form instanceof FormGroup) {
    Object.keys(form.controls).forEach((key) => {
      const control = form.controls[key]!;
      const path = parentPath ? `${parentPath}.${key}` : key;

      // Si tu veux seulement les feuilles invalides, garde ce if ;
      // si tu veux aussi les conteneurs invalides, ajoute un push ici.
      if (!(control instanceof FormGroup || control instanceof FormArray) && control.invalid) {
        invalid.push(path);
      }

      if (control instanceof FormGroup || control instanceof FormArray) {
        invalid.push(...getInvalidControls(control, path));
      }
    });
  } else if (form instanceof FormArray) {
    form.controls.forEach((control, index) => {
      const path = parentPath ? `${parentPath}[${index}]` : `[${index}]`;

      if (!(control instanceof FormGroup || control instanceof FormArray) && control.invalid) {
        invalid.push(path);
      }

      if (control instanceof FormGroup || control instanceof FormArray) {
        invalid.push(...getInvalidControls(control, path));
      }
    });
  }

  return invalid;
}
