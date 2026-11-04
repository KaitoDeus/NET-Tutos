{{- define "nettutos.labels" -}}
app.kubernetes.io/part-of: nettutos
app.kubernetes.io/managed-by: {{ .Release.Service }}
helm.sh/chart: {{ .Chart.Name }}-{{ .Chart.Version }}
{{- end -}}

{{- define "nettutos.image" -}}
{{ .root.Values.image.registry }}/{{ .root.Values.image.repository }}-{{ .svc.imageName }}:{{ .root.Values.image.tag }}
{{- end -}}

{{- define "nettutos.secretEnv" -}}
- name: {{ .name }}
  valueFrom:
    secretKeyRef:
      name: {{ .secret }}
      key: {{ .name }}
      optional: true
{{- end -}}
